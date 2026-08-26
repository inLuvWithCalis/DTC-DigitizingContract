using System.Data;
using System.Security.Cryptography;
using System.Text;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.CustomerAccess;
using ContractManagement.API.Domains.DTOs.Requests.Public;
using ContractManagement.API.Domains.DTOs.Responses.Public;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

public sealed class CustomerContractAccessService : ICustomerContractAccessService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SessionIdleLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RollingSendWindow = TimeSpan.FromMinutes(15);
    private const int RollingSendLimit = 3;
    private const int MaxOtpAttempts = 5;

    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly CustomerAccessCryptography _cryptography;
    private readonly IContractService _contractService;
    private readonly IContractAuditWriter _auditWriter;

    public CustomerContractAccessService(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant,
        CustomerAccessCryptography cryptography,
        IContractService contractService,
        IContractAuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _cryptography = cryptography;
        _contractService = contractService;
        _auditWriter = auditWriter;
    }

    public async Task<CustomerOtpRequestAcceptedResponse> RequestOtpAsync(
        string linkToken,
        string suppliedPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        var response = new CustomerOtpRequestAcceptedResponse
        {
            PublicChallengeId = _cryptography.CreatePublicChallengeId()
        };

        if (string.IsNullOrWhiteSpace(linkToken)
            || !TryNormalizePhone(suppliedPhoneNumber, out var normalizedPhone))
        {
            return response;
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var now = DateTime.UtcNow;
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var link = await _dbContext.TblContractCustomerAccessLinks
                    .SingleOrDefaultAsync(x => x.TenantId == tenantId
                        && x.TokenHash == _cryptography.HashSecret(linkToken),
                        cancellationToken);

                if (link is null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return response;
                }

                // The link can be prepared while the contract is still Draft, but it
                // is activated only when negotiation starts.
                if (!link.ActivatedAt.HasValue
                    && !link.RevokedAt.HasValue
                    && link.ExpiresAt > now)
                {
                    throw new InvalidOperationException(
                        "Chỉ có thể xem hợp đồng khi hợp đồng đang ở trạng thái đàm phán.");
                }

                if (!IsLinkActive(link, now))
                {
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.PublicAccessDenied,
                        now,
                        ContractAuditResults.Denied,
                        GetLinkFailureCode(link),
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CurrentVersionId", link.VersionId),
                            ("LinkState", "Inactive")));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return response;
                }

                var phone = await _dbContext.TblContractCustomerVerificationPhones
                    .SingleOrDefaultAsync(x =>
                        x.VerificationPhoneId == link.VerificationPhoneId
                        && x.ContractId == link.ContractId,
                        cancellationToken);

                if (phone is null
                    || !string.Equals(phone.PhoneNumberNormalized, normalizedPhone, StringComparison.Ordinal))
                {
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.PublicAccessDenied,
                        now,
                        ContractAuditResults.Denied,
                        ContractAuditFailureCodes.VerificationPhoneMismatch,
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CurrentVersionId", link.VersionId),
                            ("LinkState", "Active")));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return response;
                }

                var recent = await _dbContext.TblContractCustomerOtpChallenges
                    .Where(x => x.LinkId == link.CustomerAccessLinkId
                        && x.CreatedDate >= now - RollingSendWindow)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync(cancellationToken);

                if (recent.Count >= RollingSendLimit
                    || recent.FirstOrDefault()?.CreatedDate >= now - ResendInterval)
                {
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.PublicAccessDenied,
                        now,
                        ContractAuditResults.RateLimited,
                        ContractAuditFailureCodes.OtpRateLimited,
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CurrentVersionId", link.VersionId),
                            ("LinkState", "Active")));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return response;
                }

                var usableChallenges = await _dbContext.TblContractCustomerOtpChallenges
                    .Where(x => x.LinkId == link.CustomerAccessLinkId
                        && x.UsedAt == null
                        && x.LockedAt == null
                        && x.InvalidatedAt == null
                        && x.ExpiresAt > now)
                    .ToListAsync(cancellationToken);

                foreach (var challenge in usableChallenges)
                {
                    challenge.InvalidatedAt = now;
                }

                var otp = _cryptography.CreateOtp();
                var challengeToCreate = new TblContractCustomerOtpChallenge
                {
                    PublicChallengeId = response.PublicChallengeId,
                    LinkId = link.CustomerAccessLinkId,
                    VerificationPhoneId = link.VerificationPhoneId,
                    Purpose = "CustomerAccess",
                    OtpHash = _cryptography.HashSecret(otp),
                    ExpiresAt = now.Add(OtpLifetime),
                    FailedAttemptCount = 0,
                    CreatedDate = now
                };

                _dbContext.TblContractCustomerOtpChallenges.Add(challengeToCreate);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _dbContext.TblContractCustomerOtpDeliveryOutbox.Add(
                    new TblContractCustomerOtpDeliveryOutbox
                    {
                        ChallengeId = challengeToCreate.CustomerOtpChallengeId,
                        EncryptedPayload = _cryptography.EncryptDeliveryPayload(
                            new CustomerOtpDeliveryMessage(normalizedPhone, otp)),
                        Status = "Pending",
                        AttemptCount = 0,
                        NextAttemptAt = now,
                        CreatedDate = now
                    });
                StageSystemAudit(
                    link,
                    ContractAuditActionTypes.CustomerOtpRequested,
                    now,
                    ContractAuditResults.Succeeded,
                    subjectType: ContractAuditSubjectTypes.CustomerOtpChallenge,
                    subjectId: challengeToCreate.CustomerOtpChallengeId,
                    newValues: ContractAuditValues.Create(
                        ("LinkId", link.CustomerAccessLinkId),
                        ("CustomerOtpChallengeId", challengeToCreate.CustomerOtpChallengeId),
                        ("CurrentVersionId", link.VersionId),
                        ("ExpiresAt", challengeToCreate.ExpiresAt),
                        ("ChallengeState", "PendingDelivery")));
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<CustomerAccessSessionIssue> VerifyOtpAsync(
        string linkToken,
        string publicChallengeId,
        string otp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(linkToken)
            || string.IsNullOrWhiteSpace(publicChallengeId)
            || otp.Length != 6
            || !otp.All(char.IsAsciiDigit))
        {
            throw new UnauthorizedAccessException("OTP verification failed.");
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var now = DateTime.UtcNow;
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var committed = false;

            try
            {
                var link = await _dbContext.TblContractCustomerAccessLinks
                    .SingleOrDefaultAsync(x => x.TenantId == tenantId
                        && x.TokenHash == _cryptography.HashSecret(linkToken),
                        cancellationToken);
                if (link is null)
                {
                    throw new UnauthorizedAccessException("OTP verification failed.");
                }

                if (!IsLinkActive(link, now))
                {
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.PublicAccessDenied,
                        now,
                        ContractAuditResults.Denied,
                        GetLinkFailureCode(link),
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CurrentVersionId", link.VersionId),
                            ("LinkState", "Inactive")));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    committed = true;
                    throw new UnauthorizedAccessException("OTP verification failed.");
                }

                var contract = await _dbContext.TblContracts.SingleOrDefaultAsync(
                    x => x.ContractId == link.ContractId,
                    cancellationToken);
                if (contract is null
                    || contract.Status == (byte)ContractStatus.Cancelled
                    || contract.CurrentVersionId != link.VersionId)
                {
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.PublicAccessDenied,
                        now,
                        ContractAuditResults.Denied,
                        ContractAuditFailureCodes.VersionNoLongerCurrent,
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CurrentVersionId", link.VersionId),
                            ("LinkState", "Invalid")));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    committed = true;
                    throw new UnauthorizedAccessException("OTP verification failed.");
                }

                var challenge = await _dbContext.TblContractCustomerOtpChallenges
                    .SingleOrDefaultAsync(x =>
                        x.PublicChallengeId == publicChallengeId
                        && x.LinkId == link.CustomerAccessLinkId
                        && x.VerificationPhoneId == link.VerificationPhoneId,
                        cancellationToken);

                if (challenge is null || !IsChallengeUsable(challenge, now))
                {
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.CustomerOtpVerified,
                        now,
                        ContractAuditResults.Denied,
                        ContractAuditFailureCodes.ChallengeUnavailable,
                        subjectType: challenge is null
                            ? ContractAuditSubjectTypes.CustomerAccessLink
                            : ContractAuditSubjectTypes.CustomerOtpChallenge,
                        subjectId: challenge?.CustomerOtpChallengeId,
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CustomerOtpChallengeId", challenge?.CustomerOtpChallengeId),
                            ("CurrentVersionId", link.VersionId),
                            ("ExpiresAt", challenge?.ExpiresAt),
                            ("ChallengeState", "Unavailable"),
                            ("FailedAttemptCount", challenge?.FailedAttemptCount)));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    committed = true;
                    throw new UnauthorizedAccessException("OTP verification failed.");
                }

                var suppliedHash = _cryptography.HashSecret(otp);
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.ASCII.GetBytes(challenge.OtpHash),
                        Encoding.ASCII.GetBytes(suppliedHash)))
                {
                    challenge.FailedAttemptCount++;
                    StageSystemAudit(
                        link,
                        ContractAuditActionTypes.CustomerOtpFailed,
                        now,
                        ContractAuditResults.Denied,
                        ContractAuditFailureCodes.OtpMismatch,
                        subjectType: ContractAuditSubjectTypes.CustomerOtpChallenge,
                        subjectId: challenge.CustomerOtpChallengeId,
                        newValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CustomerOtpChallengeId", challenge.CustomerOtpChallengeId),
                            ("CurrentVersionId", link.VersionId),
                            ("ExpiresAt", challenge.ExpiresAt),
                            ("ChallengeState", "Failed"),
                            ("FailedAttemptCount", challenge.FailedAttemptCount)));
                    if (challenge.FailedAttemptCount >= MaxOtpAttempts)
                    {
                        challenge.LockedAt = now;
                        StageSystemAudit(
                            link,
                            ContractAuditActionTypes.CustomerOtpLocked,
                            now,
                            ContractAuditResults.Denied,
                            ContractAuditFailureCodes.OtpLocked,
                            subjectType: ContractAuditSubjectTypes.CustomerOtpChallenge,
                            subjectId: challenge.CustomerOtpChallengeId,
                            newValues: ContractAuditValues.Create(
                                ("LinkId", link.CustomerAccessLinkId),
                                ("CustomerOtpChallengeId", challenge.CustomerOtpChallengeId),
                                ("CurrentVersionId", link.VersionId),
                                ("ExpiresAt", challenge.ExpiresAt),
                                ("ChallengeState", "Locked"),
                                ("FailedAttemptCount", challenge.FailedAttemptCount)));
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    committed = true;
                    throw new UnauthorizedAccessException("OTP verification failed.");
                }

                challenge.UsedAt = now;
                var sessionSecret = _cryptography.CreateToken();
                var hardExpiresAt = link.ExpiresAt;
                var idleExpiresAt = Min(now.Add(SessionIdleLifetime), hardExpiresAt);
                var session = new TblContractCustomerAccessSession
                {
                    TenantId = tenantId,
                    LinkId = link.CustomerAccessLinkId,
                    ContractId = link.ContractId,
                    VersionId = link.VersionId,
                    VerificationPhoneId = link.VerificationPhoneId,
                    SessionTokenHash = _cryptography.HashSecret(sessionSecret),
                    IssuedAt = now,
                    LastActivityAt = now,
                    IdleExpiresAt = idleExpiresAt,
                    HardExpiresAt = hardExpiresAt
                };
                _dbContext.TblContractCustomerAccessSessions.Add(session);
                StageSystemAudit(
                    link,
                    ContractAuditActionTypes.CustomerOtpVerified,
                    now,
                    ContractAuditResults.Succeeded,
                    subjectType: ContractAuditSubjectTypes.CustomerOtpChallenge,
                    subjectId: challenge.CustomerOtpChallengeId,
                    newValues: ContractAuditValues.Create(
                        ("LinkId", link.CustomerAccessLinkId),
                        ("CustomerOtpChallengeId", challenge.CustomerOtpChallengeId),
                        ("CurrentVersionId", link.VersionId),
                        ("ExpiresAt", challenge.ExpiresAt),
                        ("ChallengeState", "Verified"),
                        ("FailedAttemptCount", challenge.FailedAttemptCount)));
                await _dbContext.SaveChangesAsync(cancellationToken);

                _auditWriter.StageAudits(
                [
                    new ContractAuditWriteRequest(
                        link.ContractId,
                        link.VersionId,
                        ContractAuditActorTypes.Customer,
                        null,
                        session.CustomerAccessSessionId,
                        ContractAuditActionTypes.CustomerSessionCreated,
                        ContractAuditResults.Succeeded,
                        now,
                        SubjectType: ContractAuditSubjectTypes.CustomerAccessSession,
                        SubjectId: session.CustomerAccessSessionId,
                        NewValues: ContractAuditValues.Create(
                            ("CustomerAccessSessionId", session.CustomerAccessSessionId),
                            ("SessionState", "Active"),
                            ("IdleExpiresAt", session.IdleExpiresAt),
                            ("HardExpiresAt", session.HardExpiresAt)))
                ]);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                committed = true;

                return new CustomerAccessSessionIssue(sessionSecret, idleExpiresAt);
            }
            catch
            {
                if (!committed)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
        });
    }

    public async Task<CustomerSharedContractResponse> GetSharedAsync(
        string sessionSecret,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireActiveSessionAsync(sessionSecret, cancellationToken);
        var contract = await _dbContext.TblContracts.SingleAsync(x =>
            x.ContractId == context.ContractId, cancellationToken);
        var version = await _dbContext.TblContractVersions.SingleAsync(x =>
            x.ContractId == context.ContractId && x.VersionId == context.VersionId,
            cancellationToken);

        if ((ContractStatus)contract.Status != ContractStatus.Negotiating
            || contract.CurrentVersionId != version.VersionId
            || version.IsLocked)
        {
            StageSystemAudit(
                context.Link,
                ContractAuditActionTypes.PublicAccessDenied,
                DateTime.UtcNow,
                ContractAuditResults.Denied,
                ContractAuditFailureCodes.VersionNoLongerCurrent,
                subjectType: ContractAuditSubjectTypes.CustomerAccessSession,
                subjectId: context.Session.CustomerAccessSessionId,
                newValues: ContractAuditValues.Create(
                    ("LinkId", context.Link.CustomerAccessLinkId),
                    ("CurrentVersionId", version.VersionId),
                    ("LinkState", "Invalid"),
                    ("SessionState", "Active")));
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Customer access is no longer available.");
        }

        var items = await _dbContext.TblContractItems.AsNoTracking()
            .Where(x => x.ContractId == contract.ContractId && x.VersionId == version.VersionId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.ContractItemId)
            .ToListAsync(cancellationToken);
        var terms = await _dbContext.TblContractTerms.AsNoTracking()
            .Where(x => x.ContractId == contract.ContractId && x.VersionId == version.VersionId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.TermId)
            .ToListAsync(cancellationToken);
        var comments = await _dbContext.TblContractNegotiationComments.AsNoTracking()
            .Where(x => x.ContractId == contract.ContractId && x.VersionId == version.VersionId)
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.CommentId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        RefreshSession(context.Session, now);
        _auditWriter.StageAudits(
        [
            new ContractAuditWriteRequest(
                contract.ContractId,
                version.VersionId,
                ContractAuditActorTypes.Customer,
                null,
                context.Session.CustomerAccessSessionId,
                ContractAuditActionTypes.PublicVersionViewed,
                ContractAuditResults.Succeeded,
                now,
                SubjectType: ContractAuditSubjectTypes.CustomerAccessSession,
                SubjectId: context.Session.CustomerAccessSessionId,
                NewValues: ContractAuditValues.Create(
                    ("CurrentVersionId", version.VersionId),
                    ("SessionState", "Active")))
        ]);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerSharedContractResponse
        {
            ContractCode = contract.ContractCode,
            ContractName = contract.ContractName,
            ContractNameEn = contract.ContractNameEn,
            EffectiveDate = contract.EffectiveDate,
            ExpireDate = contract.ExpireDate,
            CurrencyCode = contract.CurrencyCode,
            TotalAmount = contract.TotalAmount,
            Items = items.Select(item => new CustomerPublicContractItemResponse
            {
                ItemName = item.ItemName,
                ItemNameEn = item.ItemNameEn,
                ItemDescription = item.ItemDescription,
                Quantity = item.Quantity,
                UnitName = item.UnitName,
                LineTotal = item.LineTotal,
                DisplayOrder = item.DisplayOrder
            }).ToList(),
            Terms = terms.Select(term => new CustomerPublicContractTermResponse
            {
                TermId = term.TermId,
                TermCode = term.TermCode,
                TermTitle = term.TermTitle,
                TermTitleEn = term.TermTitleEn,
                TermContent = term.TermContent,
                TermContentEn = term.TermContentEn,
                IsNegotiable = term.IsNegotiable,
                DisplayOrder = term.DisplayOrder
            }).ToList(),
            Comments = comments.Select(MapPublicComment).ToList()
        };
    }

    public async Task<CustomerPublicNegotiationCommentResponse> CreateCommentAsync(
        string sessionSecret,
        CreateCustomerNegotiationCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await RequireActiveSessionAsync(sessionSecret, cancellationToken);
        return await _contractService.CreateCustomerCommentAsync(
            context.ContractId,
            context.VersionId,
            context.Session.CustomerAccessSessionId,
            request);
    }

    private async Task<CustomerAccessSessionContext> RequireActiveSessionAsync(
        string sessionSecret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionSecret))
        {
            throw new UnauthorizedAccessException("Customer access session is required.");
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var now = DateTime.UtcNow;
        var session = await _dbContext.TblContractCustomerAccessSessions
            .SingleOrDefaultAsync(x => x.TenantId == tenantId
                && x.SessionTokenHash == _cryptography.HashSecret(sessionSecret),
                cancellationToken);
        if (session is null)
        {
            throw new UnauthorizedAccessException("Customer access session is invalid.");
        }

        if (session.RevokedAt.HasValue
            || session.IdleExpiresAt <= now || session.HardExpiresAt <= now)
        {
            await PersistSessionDeniedAuditAsync(
                session,
                session.RevokedAt.HasValue
                    ? ContractAuditFailureCodes.SessionRevoked
                    : ContractAuditFailureCodes.SessionExpired,
                now,
                cancellationToken);
            throw new UnauthorizedAccessException("Customer access session is invalid.");
        }

        var link = await _dbContext.TblContractCustomerAccessLinks.SingleOrDefaultAsync(
            x => x.CustomerAccessLinkId == session.LinkId && x.TenantId == tenantId,
            cancellationToken);
        if (link is null || !IsLinkActive(link, now)
            || link.ContractId != session.ContractId
            || link.VersionId != session.VersionId
            || link.VerificationPhoneId != session.VerificationPhoneId)
        {
            if (link is not null)
            {
                StageSystemAudit(
                    link,
                    ContractAuditActionTypes.PublicAccessDenied,
                    now,
                    ContractAuditResults.Denied,
                    GetLinkFailureCode(link),
                    subjectType: ContractAuditSubjectTypes.CustomerAccessSession,
                    subjectId: session.CustomerAccessSessionId,
                    newValues: ContractAuditValues.Create(
                        ("LinkId", link.CustomerAccessLinkId),
                        ("CurrentVersionId", link.VersionId),
                        ("LinkState", "Inactive"),
                        ("SessionState", "Invalid")));
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            throw new UnauthorizedAccessException("Customer access session is invalid.");
        }

        return new CustomerAccessSessionContext(session, link);
    }

    private static bool IsLinkActive(
        TblContractCustomerAccessLink link,
        DateTime now) =>
        link.ActivatedAt.HasValue
        && !link.RevokedAt.HasValue
        && link.ExpiresAt > now;

    private static string GetLinkFailureCode(
        TblContractCustomerAccessLink link) => link.RevokedAt.HasValue
        ? ContractAuditFailureCodes.LinkRevoked
        : ContractAuditFailureCodes.LinkExpired;

    private async Task PersistSessionDeniedAuditAsync(
        TblContractCustomerAccessSession session,
        string failureCode,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        _auditWriter.StageAudits(
        [
            new ContractAuditWriteRequest(
                session.ContractId,
                session.VersionId,
                ContractAuditActorTypes.System,
                null,
                null,
                ContractAuditActionTypes.PublicAccessDenied,
                ContractAuditResults.Denied,
                occurredAt,
                SubjectType: ContractAuditSubjectTypes.CustomerAccessSession,
                SubjectId: session.CustomerAccessSessionId,
                NewValues: ContractAuditValues.Create(
                    ("CurrentVersionId", session.VersionId),
                    ("SessionState", "Inactive")),
                FailureCode: failureCode)
        ]);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsChallengeUsable(
        TblContractCustomerOtpChallenge challenge,
        DateTime now) =>
        challenge.ExpiresAt > now
        && !challenge.UsedAt.HasValue
        && !challenge.LockedAt.HasValue
        && !challenge.InvalidatedAt.HasValue;

    private static void RefreshSession(
        TblContractCustomerAccessSession session,
        DateTime now)
    {
        session.LastActivityAt = now;
        session.IdleExpiresAt = Min(
            now.Add(SessionIdleLifetime),
            session.HardExpiresAt);
    }

    private void StageSystemAudit(
        TblContractCustomerAccessLink link,
        string actionType,
        DateTime occurredAt,
        string result,
        string? failureCode = null,
        string? subjectType = null,
        int? subjectId = null,
        IReadOnlyDictionary<string, object?>? previousValues = null,
        IReadOnlyDictionary<string, object?>? newValues = null)
    {
        _auditWriter.StageAudits(
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
                SubjectType: subjectType ?? ContractAuditSubjectTypes.CustomerAccessLink,
                SubjectId: subjectId ?? link.CustomerAccessLinkId,
                PreviousValues: previousValues,
                NewValues: newValues,
                FailureCode: failureCode)
        ]);
    }

    private static bool TryNormalizePhone(string value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var compact = new string(value.Trim().Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (compact.StartsWith("00", StringComparison.Ordinal))
        {
            compact = "+" + compact[2..];
        }

        var digits = compact.TrimStart('+');
        if (digits.Length is < 8 or > 15 || !digits.All(char.IsAsciiDigit)
            || compact.Count(c => c == '+') > 1
            || (compact.Contains('+') && !compact.StartsWith('+')))
        {
            return false;
        }

        normalized = compact;
        return true;
    }

    private static CustomerPublicNegotiationCommentResponse MapPublicComment(
        TblContractNegotiationComment comment) => new()
    {
        CommentId = comment.CommentId,
        TermId = comment.TermId,
        ParentCommentId = comment.ParentCommentId,
        Content = comment.Content,
        Source = comment.Source,
        LifecycleState = comment.State == 0 ? "Open" : "Resolved",
        CreatedDate = comment.CreatedDate,
        UpdatedDate = comment.UpdatedDate
    };

    private static DateTime Min(DateTime left, DateTime right) =>
        left <= right ? left : right;

    private sealed record CustomerAccessSessionContext(
        TblContractCustomerAccessSession Session,
        TblContractCustomerAccessLink Link)
    {
        public int ContractId => Session.ContractId;

        public int VersionId => Session.VersionId;
    }
}
