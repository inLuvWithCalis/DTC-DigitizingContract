using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Requests.Public;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Public;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

public partial class ContractService
{
    private static readonly TimeSpan CustomerLinkLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan CustomerSessionIdleLifetime = TimeSpan.FromMinutes(30);

    public async Task<IReadOnlyList<ContractCustomerVerificationPhoneResponse>>
        GetCustomerVerificationPhonesAsync(int contractId, int employeeId)
    {
        var contract = await GetCustomerAccessContractAsync(
            contractId,
            employeeId,
            allowManagerOrAdmin: true);

        var phones = await _dbContext.TblContractCustomerVerificationPhones
            .AsNoTracking()
            .Where(x => x.ContractId == contract.ContractId)
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.VerificationPhoneId)
            .ToListAsync();

        return phones.Select(phone => MapVerificationPhone(
            phone,
            contract.CurrentVerificationPhoneId == phone.VerificationPhoneId))
            .ToList();
    }

    public async Task<ContractCustomerVerificationPhoneResponse>
        UpdateCustomerVerificationPhoneAsync(
            int contractId,
            UpdateContractCustomerVerificationPhoneRequest request,
            int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = NormalizeRequired(request.Reason, 1000, nameof(request.Reason));
        var source = NormalizePhoneSource(request.PhoneSource);
        var expectedRowVersion = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        var now = DateTime.UtcNow;
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var contract = await GetCustomerAccessContractAsync(
                    contractId,
                    employeeId,
                    allowManagerOrAdmin: true);
                if ((ContractStatus)contract.Status == ContractStatus.Cancelled)
                {
                    throw new InvalidOperationException("Cancelled contracts cannot change customer access.");
                }

                EnsureRowVersionMatches(contract.RowVersion, expectedRowVersion, "Contract");
                _dbContext.Entry(contract).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

                var normalizedPhone = await ResolveVerificationPhoneAsync(
                    contract,
                    source,
                    request.ManualPhoneNumber);
                var current = contract.CurrentVerificationPhoneId.HasValue
                    ? await _dbContext.TblContractCustomerVerificationPhones.SingleOrDefaultAsync(
                        x => x.VerificationPhoneId == contract.CurrentVerificationPhoneId.Value
                            && x.ContractId == contract.ContractId)
                    : null;

                if (current is not null
                    && current.PhoneSource == source
                    && current.PhoneNumberNormalized == normalizedPhone)
                {
                    await transaction.CommitAsync();
                    return MapVerificationPhone(current, true);
                }

                var phone = new TblContractCustomerVerificationPhone
                {
                    ContractId = contract.ContractId,
                    PhoneSource = source,
                    PhoneNumberNormalized = normalizedPhone,
                    Reason = reason,
                    CreatedByEmployeeId = employeeId,
                    CreatedDate = now
                };
                _dbContext.TblContractCustomerVerificationPhones.Add(phone);
                await _dbContext.SaveChangesAsync();

                if (contract.CurrentCustomerAccessLinkId.HasValue)
                {
                    await RevokeCustomerLinkStateAsync(
                        contract.CurrentCustomerAccessLinkId.Value,
                        employeeId,
                        now,
                        "Verification phone changed");
                    contract.CurrentCustomerAccessLinkId = null;
                }

                contract.CurrentVerificationPhoneId = phone.VerificationPhoneId;
                contract.UpdatedEmployeeId = employeeId;
                contract.UpdateDate = now;
                _contractAuditWriter.StageEmployeeAudits(
                [
                    new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        contract.CurrentVersionId,
                        employeeId,
                        current is null
                            ? ContractAuditActionTypes.VerificationPhoneSelected
                            : ContractAuditActionTypes.VerificationPhoneChanged,
                        ContractAuditResults.Succeeded,
                        now)
                ]);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return MapVerificationPhone(phone, true);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<ContractCustomerAccessLinkResponse>
        CreateCustomerAccessLinkAsync(
            int contractId,
            CreateContractCustomerAccessLinkRequest request,
            int employeeId,
            string publicBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expectedRowVersion = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        return await CreateOrReplaceCustomerAccessLinkAsync(
            contractId,
            null,
            expectedRowVersion,
            null,
            employeeId,
            publicBaseUrl,
            requireResponsible: true,
            ContractAuditActionTypes.CustomerAccessLinkCreated);
    }

    public async Task<ContractCustomerAccessLinkResponse>
        ReplaceCustomerAccessLinkAsync(
            int contractId,
            int linkId,
            ReplaceContractCustomerAccessLinkRequest request,
            int employeeId,
            string publicBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expectedRowVersion = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        var reason = NormalizeRequired(request.Reason, 1000, nameof(request.Reason));
        return await CreateOrReplaceCustomerAccessLinkAsync(
            contractId,
            linkId,
            expectedRowVersion,
            reason,
            employeeId,
            publicBaseUrl,
            requireResponsible: false,
            ContractAuditActionTypes.CustomerAccessLinkReplaced);
    }

    public async Task RevokeCustomerAccessLinkAsync(
        int contractId,
        int linkId,
        RevokeContractCustomerAccessLinkRequest request,
        int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (linkId <= 0)
        {
            throw new ArgumentException("LinkId must be positive.");
        }

        var reason = NormalizeRequired(request.Reason, 1000, nameof(request.Reason));
        var expectedRowVersion = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        var now = DateTime.UtcNow;
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var contract = await GetCustomerAccessContractAsync(contractId, employeeId, true);
                EnsureRowVersionMatches(contract.RowVersion, expectedRowVersion, "Contract");
                _dbContext.Entry(contract).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

                var link = await _dbContext.TblContractCustomerAccessLinks.SingleOrDefaultAsync(
                    x => x.CustomerAccessLinkId == linkId && x.ContractId == contract.ContractId);
                if (link is null || link.RevokedAt.HasValue)
                {
                    throw new InvalidOperationException("Customer access link is not active.");
                }

                await RevokeCustomerLinkStateAsync(linkId, employeeId, now, reason);
                if (contract.CurrentCustomerAccessLinkId == linkId)
                {
                    contract.CurrentCustomerAccessLinkId = null;
                }

                contract.UpdatedEmployeeId = employeeId;
                contract.UpdateDate = now;
                _contractAuditWriter.StageEmployeeAudits(
                [
                    new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        link.VersionId,
                        employeeId,
                        ContractAuditActionTypes.CustomerAccessLinkRevoked,
                        ContractAuditResults.Succeeded,
                        now)
                ]);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<CustomerPublicNegotiationCommentResponse>
        CreateCustomerCommentAsync(
            int contractId,
            int versionId,
            int customerAccessSessionId,
            CreateCustomerNegotiationCommentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (contractId <= 0 || versionId <= 0 || customerAccessSessionId <= 0)
        {
            throw new UnauthorizedAccessException("Customer session is invalid.");
        }

        if (request.TermId is <= 0 || request.ParentCommentId is <= 0)
        {
            throw new ArgumentException("Comment target is invalid.");
        }

        var content = NormalizeRequired(request.Content, 4000, nameof(request.Content));
        var now = DateTime.UtcNow;
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var contract = await _dbContext.TblContracts.SingleOrDefaultAsync(
                    x => x.ContractId == contractId);
                var version = await _dbContext.TblContractVersions.SingleOrDefaultAsync(
                    x => x.ContractId == contractId && x.VersionId == versionId);
                var session = await _dbContext.TblContractCustomerAccessSessions.SingleOrDefaultAsync(
                    x => x.CustomerAccessSessionId == customerAccessSessionId
                        && x.ContractId == contractId && x.VersionId == versionId);
                if (contract is null || version is null || session is null
                    || (ContractStatus)contract.Status != ContractStatus.Negotiating
                    || contract.CurrentVersionId != versionId || version.IsLocked
                    || session.RevokedAt.HasValue || session.IdleExpiresAt <= now
                    || session.HardExpiresAt <= now)
                {
                    throw new UnauthorizedAccessException("Customer comment is not allowed.");
                }

                var link = await _dbContext.TblContractCustomerAccessLinks.SingleOrDefaultAsync(
                    x => x.CustomerAccessLinkId == session.LinkId
                        && x.ContractId == contractId && x.VersionId == versionId);
                if (link is null || !IsCustomerAccessLinkActive(link, now)
                    || link.VerificationPhoneId != session.VerificationPhoneId)
                {
                    throw new UnauthorizedAccessException("Customer comment is not allowed.");
                }

                int? effectiveTermId = request.TermId;
                if (request.TermId.HasValue)
                {
                    var term = await _dbContext.TblContractTerms.SingleOrDefaultAsync(x =>
                        x.TermId == request.TermId.Value && x.ContractId == contractId
                        && x.VersionId == versionId);
                    if (term is null || !term.IsNegotiable)
                    {
                        throw new InvalidOperationException("Term is not open for negotiation.");
                    }
                }

                if (request.ParentCommentId.HasValue)
                {
                    var parent = await _dbContext.TblContractNegotiationComments.SingleOrDefaultAsync(x =>
                        x.CommentId == request.ParentCommentId.Value
                        && x.ContractId == contractId && x.VersionId == versionId);
                    if (parent is null || parent.State != 0
                        || (request.TermId.HasValue && parent.TermId != request.TermId))
                    {
                        throw new InvalidOperationException("Comment parent is not available.");
                    }

                    effectiveTermId = parent.TermId;
                }

                var comment = new TblContractNegotiationComment
                {
                    ContractId = contractId,
                    VersionId = versionId,
                    TermId = effectiveTermId,
                    ParentCommentId = request.ParentCommentId,
                    Content = content,
                    Source = "Customer",
                    CustomerAccessSessionId = session.CustomerAccessSessionId,
                    State = 0,
                    CreatedDate = now
                };
                _dbContext.TblContractNegotiationComments.Add(comment);
                SetSyntheticCommentRowVersionIfNeeded(comment);
                RefreshCustomerSession(session, now);
                await _dbContext.SaveChangesAsync();

                _dbContext.TblContractNegotiationCommentEvents.Add(
                    new TblContractNegotiationCommentEvent
                    {
                        CommentId = comment.CommentId,
                        EventType = (byte)ContractNegotiationCommentEventType.Created,
                        ActorType = ContractAuditActorTypes.Customer,
                        CustomerAccessSessionId = session.CustomerAccessSessionId,
                        OccurredAt = now
                    });
                _contractAuditWriter.StageAudits(
                [
                    new ContractAuditWriteRequest(
                        contractId,
                        versionId,
                        ContractAuditActorTypes.Customer,
                        null,
                        session.CustomerAccessSessionId,
                        request.ParentCommentId.HasValue
                            ? ContractAuditActionTypes.CustomerCommentReplyCreated
                            : ContractAuditActionTypes.CustomerCommentCreated,
                        ContractAuditResults.Succeeded,
                        now)
                ]);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return new CustomerPublicNegotiationCommentResponse
                {
                    CommentId = comment.CommentId,
                    TermId = comment.TermId,
                    ParentCommentId = comment.ParentCommentId,
                    Content = comment.Content,
                    Source = comment.Source,
                    LifecycleState = "Open",
                    CreatedDate = comment.CreatedDate
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private async Task<ContractCustomerAccessLinkResponse>
        CreateOrReplaceCustomerAccessLinkAsync(
            int contractId,
            int? linkIdToReplace,
            byte[] expectedRowVersion,
            string? replacementReason,
            int employeeId,
            string publicBaseUrl,
            bool requireResponsible,
            string auditAction)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            throw new ArgumentException("Public base URL is required.", nameof(publicBaseUrl));
        }

        var now = DateTime.UtcNow;
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var contract = await GetCustomerAccessContractAsync(
                    contractId, employeeId, !requireResponsible);
                if (requireResponsible && contract.EmployeeId != employeeId)
                {
                    throw new UnauthorizedAccessException("Only the current responsible employee can create the first link.");
                }

                if ((ContractStatus)contract.Status is not (ContractStatus.Draft or ContractStatus.Negotiating)
                    || !contract.CurrentVersionId.HasValue
                    || !contract.CurrentVerificationPhoneId.HasValue)
                {
                    throw new InvalidOperationException("Contract does not have an eligible customer access context.");
                }

                EnsureRowVersionMatches(contract.RowVersion, expectedRowVersion, "Contract");
                _dbContext.Entry(contract).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

                var version = await _dbContext.TblContractVersions.SingleOrDefaultAsync(x =>
                    x.ContractId == contract.ContractId && x.VersionId == contract.CurrentVersionId.Value);
                if (version is null || version.IsLocked)
                {
                    throw new InvalidOperationException("Current contract version is locked.");
                }

                if (linkIdToReplace.HasValue)
                {
                    var previous = await _dbContext.TblContractCustomerAccessLinks.SingleOrDefaultAsync(x =>
                        x.CustomerAccessLinkId == linkIdToReplace.Value
                        && x.ContractId == contract.ContractId && x.RevokedAt == null);
                    if (previous is null)
                    {
                        throw new InvalidOperationException("Customer access link is not active.");
                    }

                    await RevokeCustomerLinkStateAsync(
                        previous.CustomerAccessLinkId,
                        employeeId,
                        now,
                        replacementReason ?? "Replaced");
                }
                else if (contract.CurrentCustomerAccessLinkId.HasValue)
                {
                    var existing = await _dbContext.TblContractCustomerAccessLinks.SingleOrDefaultAsync(x =>
                        x.CustomerAccessLinkId == contract.CurrentCustomerAccessLinkId.Value
                        && x.RevokedAt == null);
                    if (existing is not null)
                    {
                        throw new InvalidOperationException("An active customer access link already exists.");
                    }
                }

                var tenant = RequireCurrentTenant();
                var rawToken = _customerAccessCryptography!.CreateToken();
                var link = new TblContractCustomerAccessLink
                {
                    TenantId = tenant.TenantId,
                    ContractId = contract.ContractId,
                    VersionId = version.VersionId,
                    VerificationPhoneId = contract.CurrentVerificationPhoneId.Value,
                    TokenHash = _customerAccessCryptography.HashSecret(rawToken),
                    CreatedByEmployeeId = employeeId,
                    CreatedDate = now,
                    ActivatedAt = (ContractStatus)contract.Status == ContractStatus.Negotiating ? now : null,
                    ExpiresAt = now.Add(CustomerLinkLifetime)
                };
                _dbContext.TblContractCustomerAccessLinks.Add(link);
                await _dbContext.SaveChangesAsync();

                contract.CurrentCustomerAccessLinkId = link.CustomerAccessLinkId;
                contract.UpdatedEmployeeId = employeeId;
                contract.UpdateDate = now;
                _contractAuditWriter.StageEmployeeAudits(
                [
                    new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        version.VersionId,
                        employeeId,
                        auditAction,
                        ContractAuditResults.Succeeded,
                        now)
                ]);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ContractCustomerAccessLinkResponse
                {
                    LinkId = link.CustomerAccessLinkId,
                    State = link.ActivatedAt.HasValue ? "Active" : "PendingActivation",
                    ExpiresAt = link.ExpiresAt,
                    PublicUrl = BuildPublicUrl(publicBaseUrl, tenant.TenantCode, rawToken)
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private async Task<TblContract> GetCustomerAccessContractAsync(
        int contractId,
        int employeeId,
        bool allowManagerOrAdmin)
    {
        if (contractId <= 0 || employeeId <= 0)
        {
            throw new UnauthorizedAccessException("Employee identity is invalid.");
        }

        var contract = await _dbContext.TblContracts.SingleOrDefaultAsync(
            x => x.ContractId == contractId);
        if (contract is null)
        {
            throw new KeyNotFoundException("Contract was not found.");
        }

        if (contract.EmployeeId == employeeId)
        {
            return contract;
        }

        if (!allowManagerOrAdmin)
        {
            throw new UnauthorizedAccessException("Current responsible employee is required.");
        }

        var actor = await _dbContext.TblEmployees.AsNoTracking().SingleOrDefaultAsync(
            x => x.EmployeeId == employeeId && x.Status == ActiveEmployeeStatus);
        if (actor is null || (actor.EmployeeType != (byte)EmployeeType.Manager
            && actor.EmployeeType != (byte)EmployeeType.AdminOfficer))
        {
            throw new UnauthorizedAccessException("Employee is not authorized for customer access.");
        }

        return contract;
    }

    private async Task<string> ResolveVerificationPhoneAsync(
        TblContract contract,
        string source,
        string? manualPhoneNumber)
    {
        string? selected = source == "Manual"
            ? manualPhoneNumber
            : await _dbContext.TblCustomers.AsNoTracking()
                .Where(x => x.CustomerId == contract.CustomerId)
                .Select(x => source == "CustomerMobile" ? x.CustomerMobile : x.CustomerPhone)
                .SingleOrDefaultAsync();
        if (!TryNormalizePhone(selected, out var normalized))
        {
            throw new InvalidOperationException("Selected verification phone is unavailable or invalid.");
        }

        return normalized;
    }

    private async Task RevokeCustomerLinkStateAsync(
        int linkId,
        int revokedByEmployeeId,
        DateTime now,
        string reason)
    {
        var link = await _dbContext.TblContractCustomerAccessLinks.SingleOrDefaultAsync(
            x => x.CustomerAccessLinkId == linkId);
        if (link is null || link.RevokedAt.HasValue)
        {
            return;
        }

        link.RevokedAt = now;
        link.RevokedByEmployeeId = revokedByEmployeeId;
        link.RevocationReason = reason;
        var challenges = await _dbContext.TblContractCustomerOtpChallenges
            .Where(x => x.LinkId == linkId && x.InvalidatedAt == null)
            .ToListAsync();
        foreach (var challenge in challenges)
        {
            challenge.InvalidatedAt = now;
        }

        var sessions = await _dbContext.TblContractCustomerAccessSessions
            .Where(x => x.LinkId == linkId && x.RevokedAt == null)
            .ToListAsync();
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.RevocationReason = reason;
        }
    }

    private static bool IsCustomerAccessLinkActive(
        TblContractCustomerAccessLink link,
        DateTime now) => link.ActivatedAt.HasValue
            && link.RevokedAt == null && link.ExpiresAt > now;

    private static void RefreshCustomerSession(
        TblContractCustomerAccessSession session,
        DateTime now)
    {
        session.LastActivityAt = now;
        session.IdleExpiresAt = now.Add(CustomerSessionIdleLifetime) <= session.HardExpiresAt
            ? now.Add(CustomerSessionIdleLifetime)
            : session.HardExpiresAt;
    }

    private static ContractCustomerVerificationPhoneResponse MapVerificationPhone(
        TblContractCustomerVerificationPhone phone,
        bool isCurrent) => new()
    {
        VerificationPhoneId = phone.VerificationPhoneId,
        PhoneSource = phone.PhoneSource,
        MaskedPhoneNumber = MaskPhone(phone.PhoneNumberNormalized),
        IsCurrent = isCurrent,
        CreatedDate = phone.CreatedDate,
        RowVersion = EncodeRowVersion(phone.RowVersion)
    };

    private static string NormalizePhoneSource(string source) => source?.Trim() switch
    {
        "CustomerMobile" => "CustomerMobile",
        "CustomerPhone" => "CustomerPhone",
        "Manual" => "Manual",
        _ => throw new ArgumentException("PhoneSource is invalid.", nameof(source))
    };

    private static bool TryNormalizePhone(string? value, out string normalized)
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

    private static string MaskPhone(string normalized)
    {
        var visible = Math.Min(4, normalized.Length);
        return new string('*', normalized.Length - visible) + normalized[^visible..];
    }

    private static string NormalizeRequired(string? value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} is required and too long.", parameterName);
        }

        return normalized;
    }

    private ContractManagement.Infrastructure.MultiTenancy.Models.ResolvedTenant RequireCurrentTenant() =>
        _currentTenant?.GetRequiredTenant()
        ?? throw new InvalidOperationException("Customer access services require a current tenant.");

    private static string BuildPublicUrl(string baseUrl, string tenantCode, string rawToken) =>
        $"{baseUrl.TrimEnd('/')}/public/contracts/{Uri.EscapeDataString(tenantCode)}/{rawToken}";
}
