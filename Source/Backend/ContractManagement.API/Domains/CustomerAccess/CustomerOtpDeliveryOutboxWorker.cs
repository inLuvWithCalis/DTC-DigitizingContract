using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractManagement.API.Domains.CustomerAccess;

/// <summary>
/// Leases encrypted OTP outbox rows tenant-by-tenant. It never logs the payload.
/// </summary>
public sealed class CustomerOtpDeliveryOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CustomerOtpOptions _options;
    private readonly ILogger<CustomerOtpDeliveryOutboxWorker> _logger;

    public CustomerOtpDeliveryOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CustomerOtpOptions> options,
        ILogger<CustomerOtpDeliveryOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tenants = await ReadActiveTenantsAsync(stoppingToken);
                foreach (var tenant in tenants)
                {
                    await ProcessTenantAsync(tenant, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Customer OTP outbox worker cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task<IReadOnlyList<ResolvedTenant>> ReadActiveTenantsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var central = scope.ServiceProvider.GetRequiredService<CentralDbContext>();
        return await central.Tenants.AsNoTracking()
            .Where(x => x.Status == TenantStatus.Active)
            .Include(x => x.TenantDatabase)
            .Select(x => new ResolvedTenant(
                x.TenantId,
                x.TenantCode,
                x.TenantName,
                x.TenantDatabase.Mode,
                x.TenantDatabase.ConnectionString))
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessTenantAsync(
        ResolvedTenant tenant,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<DbDtctechContext>();
        var cryptography = scope.ServiceProvider.GetRequiredService<CustomerAccessCryptography>();
        var deliveryProvider = scope.ServiceProvider.GetRequiredService<ICustomerOtpDeliveryProvider>();
        var auditWriter = scope.ServiceProvider.GetRequiredService<IContractAuditWriter>();
        var now = DateTime.UtcNow;
        var candidates = await dbContext.TblContractCustomerOtpDeliveryOutbox
            .Where(x => (x.Status == "Pending" && x.NextAttemptAt <= now)
                || (x.Status == "Leased" && x.LeaseUntil <= now))
            .OrderBy(x => x.CreatedDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var leaseId = Guid.NewGuid().ToString("N");
            candidate.Status = "Leased";
            candidate.LeaseId = leaseId;
            candidate.LeaseUntil = now.AddMinutes(1);
            candidate.AttemptCount++;

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
                continue;
            }

            var challenge = await dbContext.TblContractCustomerOtpChallenges
                .SingleOrDefaultAsync(x => x.CustomerOtpChallengeId == candidate.ChallengeId,
                    cancellationToken);
            if (challenge is null || challenge.ExpiresAt <= DateTime.UtcNow
                || challenge.InvalidatedAt.HasValue || challenge.LockedAt.HasValue)
            {
                candidate.Status = "Failed";
                candidate.FailedAt = DateTime.UtcNow;
                candidate.LeaseId = null;
                candidate.LeaseUntil = null;
                if (challenge is not null && !challenge.InvalidatedAt.HasValue)
                {
                    challenge.InvalidatedAt = DateTime.UtcNow;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                var message = cryptography.DecryptDeliveryPayload(candidate.EncryptedPayload);
                if (challenge.ExpiresAt <= DateTime.UtcNow)
                {
                    throw new InvalidOperationException("OTP challenge expired before delivery.");
                }

                await deliveryProvider.DeliverAsync(message, cancellationToken);
                var sentAt = DateTime.UtcNow;
                candidate.Status = "Sent";
                candidate.SentAt = sentAt;
                candidate.LeaseId = null;
                candidate.LeaseUntil = null;
                challenge.SentAt = sentAt;
                await StageDeliveryAuditAsync(
                    dbContext,
                    auditWriter,
                    challenge,
                    candidate.CustomerOtpDeliveryOutboxId,
                    ContractAuditActionTypes.CustomerOtpSent,
                    ContractAuditResults.Succeeded,
                    sentAt,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var failedAt = DateTime.UtcNow;
                candidate.LeaseId = null;
                candidate.LeaseUntil = null;
                candidate.LastFailure = exception.GetType().Name;
                if (candidate.AttemptCount >= _options.MaxDeliveryAttempts
                    || challenge.ExpiresAt <= failedAt)
                {
                    candidate.Status = "Failed";
                    candidate.FailedAt = failedAt;
                    challenge.InvalidatedAt ??= failedAt;
                }
                else
                {
                    candidate.Status = "Pending";
                    candidate.NextAttemptAt = failedAt.AddSeconds(
                        Math.Max(1, _options.RetryDelaySeconds));
                }

                await StageDeliveryAuditAsync(
                    dbContext,
                    auditWriter,
                    challenge,
                    candidate.CustomerOtpDeliveryOutboxId,
                    ContractAuditActionTypes.CustomerOtpFailed,
                    ContractAuditResults.Failed,
                    failedAt,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static async Task StageDeliveryAuditAsync(
        DbDtctechContext dbContext,
        IContractAuditWriter auditWriter,
        TblContractCustomerOtpChallenge challenge,
        int outboxId,
        string actionType,
        string result,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var link = await dbContext.TblContractCustomerAccessLinks.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CustomerAccessLinkId == challenge.LinkId,
                cancellationToken);
        if (link is null)
        {
            return;
        }

        auditWriter.StageAudits(
        [
            new ContractAuditWriteRequest(
                link.ContractId,
                link.VersionId,
                ContractAuditActorTypes.System,
                null,
                null,
                actionType,
                result,
                occurredAt,
                SubjectType: ContractAuditSubjectTypes.CustomerOtpChallenge,
                SubjectId: challenge.CustomerOtpChallengeId,
                NewValues: ContractAuditValues.Create(
                    ("LinkId", link.CustomerAccessLinkId),
                    ("CustomerOtpChallengeId", challenge.CustomerOtpChallengeId),
                    ("CurrentVersionId", link.VersionId),
                    ("ExpiresAt", challenge.ExpiresAt),
                    ("ChallengeState", result == ContractAuditResults.Succeeded
                        ? "Sent"
                        : "DeliveryFailed"),
                    ("FailedAttemptCount", challenge.FailedAttemptCount)),
                FailureCode: result == ContractAuditResults.Failed
                    ? ContractAuditFailureCodes.OtpDeliveryFailed
                    : null,
                CorrelationId: $"customer-otp-outbox-{outboxId}")
        ]);
    }
}
