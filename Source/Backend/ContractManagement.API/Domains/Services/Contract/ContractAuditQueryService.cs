using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Read model for staff audit visibility. It never exposes audit storage to a
/// customer endpoint and applies authorization before returning any record.
/// </summary>
public sealed class ContractAuditQueryService : IContractAuditQueryService
{
    private const byte ActiveEmployeeStatus = 1;
    private const int MaxCsvRows = 50_000;

    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public ContractAuditQueryService(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<ContractAuditCursorPageResponse> QueryAsync(
        ContractAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var (query, tenantId) = await BuildAuthorizedQueryAsync(
            filter,
            employeeId,
            cancellationToken);
        var totalCount = await query.CountAsync(cancellationToken);

        var cursor = DecodeCursor(filter.Cursor);
        if (cursor is not null)
        {
            query = query.Where(audit =>
                audit.OccurredAt < cursor.OccurredAt
                || audit.OccurredAt == cursor.OccurredAt
                    && audit.ContractAuditId < cursor.ContractAuditId);
        }

        var records = await query
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.ContractAuditId)
            .Take(filter.PageSize + 1)
            .ToListAsync(cancellationToken);
        var hasMore = records.Count > filter.PageSize;
        if (hasMore)
        {
            records.RemoveAt(records.Count - 1);
        }

        var lookup = await LoadLookupContextAsync(
            records,
            tenantId,
            cancellationToken);
        var items = records.Select(audit => Map(audit, lookup)).ToList();
        var last = records.LastOrDefault();

        return new ContractAuditCursorPageResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = filter.PageSize,
            HasMore = hasMore,
            NextCursor = hasMore && last is not null
                ? EncodeCursor(last)
                : null
        };
    }

    public async Task<ContractAuditExportFile> ExportCsvAsync(
        ContractAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var (query, tenantId) = await BuildAuthorizedQueryAsync(
            filter,
            employeeId,
            cancellationToken);
        var records = await query
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.ContractAuditId)
            .Take(MaxCsvRows + 1)
            .ToListAsync(cancellationToken);
        if (records.Count > MaxCsvRows)
        {
            throw new ArgumentException(
                $"Kết quả xuất vượt quá {MaxCsvRows:N0} bản ghi. Hãy thu hẹp bộ lọc.");
        }

        var lookup = await LoadLookupContextAsync(
            records,
            tenantId,
            cancellationToken);
        var rows = records.Select(audit => Map(audit, lookup)).ToList();
        var content = Encoding.UTF8.GetBytes("\uFEFF" + BuildCsv(rows));
        var fileName = $"contract-audits-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return new ContractAuditExportFile(content, fileName);
    }

    private async Task<(IQueryable<TblContractAudit> Query, int TenantId)>
        BuildAuthorizedQueryAsync(
            ContractAuditFilterRequest filter,
            int employeeId,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValidateFilter(filter);

        var actor = await _dbContext.TblEmployees.AsNoTracking()
            .SingleOrDefaultAsync(
                employee => employee.EmployeeId == employeeId
                    && employee.Status == ActiveEmployeeStatus,
                cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "Employee is not authorized to view contract audits.");

        var canViewTenant = actor.EmployeeType == (byte)EmployeeType.Manager
            || actor.EmployeeType == (byte)EmployeeType.AdminOfficer;
        if (!canViewTenant)
        {
            if (filter.ContractId is not > 0)
            {
                throw new UnauthorizedAccessException(
                    "Responsible employee must select a contract.");
            }

            var isCurrentResponsible = await _dbContext.TblContracts.AsNoTracking()
                .AnyAsync(
                    contract => contract.ContractId == filter.ContractId.Value
                        && contract.EmployeeId == employeeId,
                    cancellationToken);
            if (!isCurrentResponsible)
            {
                throw new UnauthorizedAccessException(
                    "Only the current responsible employee may view this audit.");
            }
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var query = _dbContext.TblContractAudits.AsNoTracking()
            .Where(audit => audit.TenantId == tenantId);

        if (filter.ContractId.HasValue)
        {
            query = query.Where(audit => audit.ContractId == filter.ContractId.Value);
        }

        if (filter.VersionId.HasValue)
        {
            query = query.Where(audit => audit.VersionId == filter.VersionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorType))
        {
            var actorType = filter.ActorType.Trim();
            query = query.Where(audit => audit.ActorType == actorType);
        }

        if (filter.ActorEmployeeId.HasValue)
        {
            query = query.Where(audit =>
                audit.ActorEmployeeId == filter.ActorEmployeeId.Value);
        }

        if (filter.ActorCustomerAccessSessionId.HasValue)
        {
            query = query.Where(audit => audit.ActorCustomerAccessSessionId
                == filter.ActorCustomerAccessSessionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
        {
            var actionType = filter.ActionType.Trim();
            query = query.Where(audit => audit.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Result))
        {
            var result = filter.Result.Trim();
            query = query.Where(audit => audit.Result == result);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            var correlationId = filter.CorrelationId.Trim();
            query = query.Where(audit => audit.CorrelationId == correlationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SubjectType))
        {
            var subjectType = filter.SubjectType.Trim();
            query = query.Where(audit => audit.SubjectType == subjectType);
        }

        if (filter.SubjectId.HasValue)
        {
            query = query.Where(audit => audit.SubjectId == filter.SubjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.FailureCode))
        {
            var failureCode = filter.FailureCode.Trim();
            query = query.Where(audit => audit.FailureCode == failureCode);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt <= filter.ToUtc.Value);
        }

        return (query, tenantId);
    }

    private async Task<AuditLookupContext> LoadLookupContextAsync(
        IReadOnlyCollection<TblContractAudit> records,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var employeeIds = records
            .Where(audit => audit.ActorEmployeeId.HasValue
                && string.IsNullOrWhiteSpace(audit.ActorDisplayNameSnapshot))
            .Select(audit => audit.ActorEmployeeId!.Value)
            .Distinct()
            .ToList();
        var employeeNames = new Dictionary<int, string?>();
        if (employeeIds.Count > 0)
        {
            var employees = await _dbContext.TblEmployees.AsNoTracking()
                .Where(employee => employeeIds.Contains(employee.EmployeeId))
                .Select(employee => new
                {
                    employee.EmployeeId,
                    employee.EmployeeFullName,
                    employee.EmployeeCode,
                    employee.EmployeeAccount
                })
                .ToListAsync(cancellationToken);
            employeeNames = employees.ToDictionary(
                employee => employee.EmployeeId,
                employee => FirstNonEmpty(
                    employee.EmployeeFullName,
                    employee.EmployeeCode,
                    employee.EmployeeAccount));
        }

        var contractIds = records
            .Where(audit => string.IsNullOrWhiteSpace(audit.ContractNameSnapshot)
                || audit.ActorCustomerAccessSessionId.HasValue
                    && string.IsNullOrWhiteSpace(audit.ActorDisplayNameSnapshot))
            .Select(audit => audit.ContractId)
            .Distinct()
            .ToList();
        var contracts = new Dictionary<int, ContractLookup>();
        if (contractIds.Count > 0)
        {
            var contractRows = await (
                from contract in _dbContext.TblContracts.AsNoTracking()
                join customer in _dbContext.TblCustomers.AsNoTracking()
                    on contract.CustomerId equals customer.CustomerId into customers
                from customer in customers.DefaultIfEmpty()
                where contractIds.Contains(contract.ContractId)
                select new
                {
                    contract.ContractId,
                    contract.ContractCode,
                    contract.ContractName,
                    CustomerName = customer == null
                        ? null
                        : customer.CustomerFullName
                            ?? customer.CustomerCompany
                            ?? customer.CustomerRepresentativeName
                            ?? customer.CustomerCode
                })
                .ToListAsync(cancellationToken);
            contracts = contractRows.ToDictionary(
                contract => contract.ContractId,
                contract => new ContractLookup(
                    contract.ContractCode,
                    contract.ContractName,
                    contract.CustomerName));
        }

        var versionIds = records.Where(audit => audit.VersionId.HasValue
                && !audit.VersionNoSnapshot.HasValue)
            .Select(audit => audit.VersionId!.Value)
            .Distinct()
            .ToList();
        var versionNumbers = versionIds.Count == 0
            ? new Dictionary<int, int>()
            : await _dbContext.TblContractVersions.AsNoTracking()
                .Where(version => versionIds.Contains(version.VersionId))
                .ToDictionaryAsync(
                    version => version.VersionId,
                    version => version.VersionNo,
                    cancellationToken);

        var sessionIds = records
            .Where(audit => audit.ActorCustomerAccessSessionId.HasValue
                && (string.IsNullOrWhiteSpace(audit.ActorDisplayNameSnapshot)
                    || string.IsNullOrWhiteSpace(audit.ActorMaskedPhoneSnapshot)
                    || string.IsNullOrWhiteSpace(audit.ActorPhoneSourceSnapshot)))
            .Select(audit => audit.ActorCustomerAccessSessionId!.Value)
            .Distinct()
            .ToList();
        var sessions = sessionIds.Count == 0
            ? new List<SessionLookup>()
            : await _dbContext.TblContractCustomerAccessSessions
                .AsNoTracking()
                .Where(session => session.TenantId == tenantId
                    && sessionIds.Contains(session.CustomerAccessSessionId))
                .Select(session => new SessionLookup(
                    session.CustomerAccessSessionId,
                    session.ContractId,
                    session.VerificationPhoneId))
                .ToListAsync(cancellationToken);
        var phoneIds = sessions.Select(session => session.VerificationPhoneId)
            .Distinct()
            .ToList();
        var phones = new Dictionary<int, PhoneLookup>();
        if (phoneIds.Count > 0)
        {
            var phoneRows = await _dbContext.TblContractCustomerVerificationPhones
                .AsNoTracking()
                .Where(phone => phoneIds.Contains(phone.VerificationPhoneId))
                .Select(phone => new
                {
                    phone.VerificationPhoneId,
                    phone.PhoneNumberNormalized,
                    phone.PhoneSource
                })
                .ToListAsync(cancellationToken);
            phones = phoneRows.ToDictionary(
                phone => phone.VerificationPhoneId,
                phone => new PhoneLookup(
                    MaskPhone(phone.PhoneNumberNormalized),
                    phone.PhoneSource));
        }
        var customerActors = sessions.ToDictionary(
            session => session.CustomerAccessSessionId,
            session =>
            {
                contracts.TryGetValue(session.ContractId, out var contract);
                phones.TryGetValue(session.VerificationPhoneId, out var phone);
                return new CustomerActorLookup(
                    contract?.CustomerName,
                    phone?.MaskedPhone,
                    phone?.PhoneSource);
            });

        return new AuditLookupContext(
            employeeNames,
            customerActors,
            contracts,
            versionNumbers);
    }

    private static ContractAuditResponse Map(
        TblContractAudit audit,
        AuditLookupContext lookup)
    {
        lookup.Contracts.TryGetValue(audit.ContractId, out var contract);
        CustomerActorLookup? customerActor = null;
        if (audit.ActorCustomerAccessSessionId.HasValue)
        {
            lookup.CustomerActors.TryGetValue(
                audit.ActorCustomerAccessSessionId.Value,
                out customerActor);
        }

        string? employeeName = null;
        if (audit.ActorEmployeeId.HasValue)
        {
            lookup.EmployeeNames.TryGetValue(
                audit.ActorEmployeeId.Value,
                out employeeName);
        }

        int? versionNo = audit.VersionNoSnapshot;
        if (!versionNo.HasValue && audit.VersionId.HasValue
            && lookup.VersionNumbers.TryGetValue(
                audit.VersionId.Value,
                out var currentVersionNo))
        {
            versionNo = currentVersionNo;
        }

        return new ContractAuditResponse
        {
            ContractAuditId = audit.ContractAuditId,
            ContractId = audit.ContractId,
            VersionId = audit.VersionId,
            VersionNo = versionNo,
            ContractCode = audit.ContractCodeSnapshot ?? contract?.ContractCode,
            ContractName = audit.ContractNameSnapshot ?? contract?.ContractName,
            SubjectType = audit.SubjectType ?? ContractAuditSubjectTypes.Contract,
            SubjectId = audit.SubjectId ?? audit.ContractId,
            ActorType = audit.ActorType,
            ActorEmployeeId = audit.ActorEmployeeId,
            ActorCustomerAccessSessionId = audit.ActorCustomerAccessSessionId,
            ActorDisplayName = audit.ActorDisplayNameSnapshot
                ?? employeeName
                ?? customerActor?.DisplayName
                ?? contract?.CustomerName,
            ActorMaskedPhone = audit.ActorMaskedPhoneSnapshot
                ?? customerActor?.MaskedPhone,
            ActorPhoneSource = audit.ActorPhoneSourceSnapshot
                ?? customerActor?.PhoneSource,
            ActionType = audit.ActionType,
            Result = audit.Result,
            FailureCode = audit.FailureCode,
            PreviousValues = ParseValues(audit.PreviousValuesJson),
            NewValues = ParseValues(audit.NewValuesJson),
            Reason = audit.Reason,
            OccurredAt = audit.OccurredAt,
            IpAddress = audit.IpAddress,
            UserAgent = audit.UserAgent,
            CorrelationId = audit.CorrelationId
        };
    }

    private static string BuildCsv(IReadOnlyCollection<ContractAuditResponse> rows)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder,
            "OccurredAtUtc", "AuditId", "ContractCode", "ContractName",
            "ContractId", "VersionNo", "VersionId", "ActorType",
            "ActorDisplayName", "ActorEmployeeId", "CustomerSessionId",
            "MaskedPhone", "PhoneSource", "ActionType", "Result",
            "FailureCode", "SubjectType", "SubjectId", "Reason",
            "IpAddress", "UserAgent", "CorrelationId", "PreviousValues",
            "NewValues");
        foreach (var row in rows)
        {
            AppendCsvRow(builder,
                row.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
                row.ContractAuditId,
                row.ContractCode,
                row.ContractName,
                row.ContractId,
                row.VersionNo,
                row.VersionId,
                row.ActorType,
                row.ActorDisplayName,
                row.ActorEmployeeId,
                row.ActorCustomerAccessSessionId,
                row.ActorMaskedPhone,
                row.ActorPhoneSource,
                row.ActionType,
                row.Result,
                row.FailureCode,
                row.SubjectType,
                row.SubjectId,
                row.Reason,
                row.IpAddress,
                row.UserAgent,
                row.CorrelationId,
                row.PreviousValues is null
                    ? null
                    : JsonSerializer.Serialize(row.PreviousValues),
                row.NewValues is null
                    ? null
                    : JsonSerializer.Serialize(row.NewValues));
        }

        return builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, params object?[] values)
    {
        builder.AppendLine(string.Join(',', values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            text = "'" + text;
        }

        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static string EncodeCursor(TblContractAudit audit)
    {
        var value = $"{audit.OccurredAt.Ticks}:{audit.ContractAuditId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static AuditCursor? DecodeCursor(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        try
        {
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(encoded.Trim()));
            var parts = value.Split(':', 2);
            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var ticks)
                || !int.TryParse(parts[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var auditId)
                || ticks < DateTime.MinValue.Ticks
                || ticks > DateTime.MaxValue.Ticks
                || auditId <= 0)
            {
                throw new FormatException();
            }

            return new AuditCursor(
                new DateTime(ticks, DateTimeKind.Utc),
                auditId);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Audit cursor is invalid.", nameof(encoded), exception);
        }
    }

    private static Dictionary<string, JsonElement>? ParseValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? MaskPhone(string? normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var visible = Math.Min(4, normalized.Length);
        return new string('*', normalized.Length - visible) + normalized[^visible..];
    }

    private static void ValidateFilter(ContractAuditFilterRequest filter)
    {
        if (filter.ContractId is <= 0
            || filter.VersionId is <= 0
            || filter.ActorEmployeeId is <= 0
            || filter.ActorCustomerAccessSessionId is <= 0
            || filter.SubjectId is <= 0)
        {
            throw new ArgumentException("Audit identifiers must be positive.");
        }

        if (filter.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be from 1 to 100.");
        }

        if (filter.FromUtc.HasValue
                && filter.FromUtc.Value.Kind != DateTimeKind.Utc
            || filter.ToUtc.HasValue
                && filter.ToUtc.Value.Kind != DateTimeKind.Utc
            || filter.FromUtc > filter.ToUtc)
        {
            throw new ArgumentException("Audit time range must be valid UTC.");
        }

        ValidateLength(filter.CorrelationId, "CorrelationId", 100);
        ValidateLength(filter.FailureCode, "FailureCode", 64);
        ValidateLength(filter.Cursor, "Cursor", 256);
        ValidateValue(filter.ActorType, "ActorType", ActorTypes());
        ValidateValue(filter.Result, "Result", Results());
        ValidateValue(filter.ActionType, "ActionType", ActionTypes());
        ValidateValue(filter.SubjectType, "SubjectType", SubjectTypes());
        _ = DecodeCursor(filter.Cursor);
    }

    private static void ValidateLength(string? value, string name, int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentException($"{name} is too long.");
        }
    }

    private static void ValidateValue(
        string? value,
        string name,
        HashSet<string> allowed)
    {
        if (!string.IsNullOrWhiteSpace(value) && !allowed.Contains(value.Trim()))
        {
            throw new ArgumentException($"{name} is invalid.");
        }
    }

    private static HashSet<string> ActorTypes() =>
        [ContractAuditActorTypes.Employee, ContractAuditActorTypes.Customer,
            ContractAuditActorTypes.System];

    private static HashSet<string> SubjectTypes() =>
        [ContractAuditSubjectTypes.Contract, ContractAuditSubjectTypes.ContractVersion,
            ContractAuditSubjectTypes.NegotiationComment,
            ContractAuditSubjectTypes.CustomerAccessLink,
            ContractAuditSubjectTypes.CustomerOtpChallenge,
            ContractAuditSubjectTypes.CustomerAccessSession];

    private static HashSet<string> Results() =>
        [ContractAuditResults.Succeeded, ContractAuditResults.Failed,
            ContractAuditResults.Denied, ContractAuditResults.RateLimited,
            ContractAuditResults.ConcurrencyConflict];

    private static HashSet<string> ActionTypes() =>
    [
        ContractAuditActionTypes.ContractCreated,
        ContractAuditActionTypes.ResponsibleAssigned,
        ContractAuditActionTypes.ResponsibilityTransferred,
        ContractAuditActionTypes.DraftUpdated,
        ContractAuditActionTypes.ApprovalSubmitted,
        ContractAuditActionTypes.ContractAttachmentUploaded,
        ContractAuditActionTypes.ContractAttachmentDeleted,
        ContractAuditActionTypes.NegotiationStarted,
        ContractAuditActionTypes.NegotiationRoundCreated,
        ContractAuditActionTypes.ExternalFeedbackCreated,
        ContractAuditActionTypes.NegotiationReplyCreated,
        ContractAuditActionTypes.NegotiationCommentResolved,
        ContractAuditActionTypes.NegotiationCommentReopened,
        ContractAuditActionTypes.VerificationPhoneSelected,
        ContractAuditActionTypes.VerificationPhoneChanged,
        ContractAuditActionTypes.CustomerAccessLinkCreated,
        ContractAuditActionTypes.CustomerAccessLinkReplaced,
        ContractAuditActionTypes.CustomerAccessLinkRevoked,
        ContractAuditActionTypes.CustomerAccessLinkActivated,
        ContractAuditActionTypes.CustomerAccessLinkInvalidated,
        ContractAuditActionTypes.CustomerOtpRequested,
        ContractAuditActionTypes.CustomerOtpSent,
        ContractAuditActionTypes.CustomerOtpFailed,
        ContractAuditActionTypes.CustomerOtpLocked,
        ContractAuditActionTypes.CustomerOtpVerified,
        ContractAuditActionTypes.CustomerSessionCreated,
        ContractAuditActionTypes.CustomerSessionRevoked,
        ContractAuditActionTypes.PublicVersionViewed,
        ContractAuditActionTypes.CustomerCommentCreated,
        ContractAuditActionTypes.CustomerCommentReplyCreated,
        ContractAuditActionTypes.PublicAccessDenied,
        ContractAuditActionTypes.ConcurrencyConflict
    ];

    private sealed record AuditCursor(DateTime OccurredAt, int ContractAuditId);

    private sealed record PhoneLookup(string? MaskedPhone, string? PhoneSource);

    private sealed record SessionLookup(
        int CustomerAccessSessionId,
        int ContractId,
        int VerificationPhoneId);

    private sealed record CustomerActorLookup(
        string? DisplayName,
        string? MaskedPhone,
        string? PhoneSource);

    private sealed record ContractLookup(
        string? ContractCode,
        string? ContractName,
        string? CustomerName);

    private sealed record AuditLookupContext(
        IReadOnlyDictionary<int, string?> EmployeeNames,
        IReadOnlyDictionary<int, CustomerActorLookup> CustomerActors,
        IReadOnlyDictionary<int, ContractLookup> Contracts,
        IReadOnlyDictionary<int, int> VersionNumbers);
}
