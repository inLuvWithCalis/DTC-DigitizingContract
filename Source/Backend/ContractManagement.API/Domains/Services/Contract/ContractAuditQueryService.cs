using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Read model for staff audit visibility. It never exposes audit storage to a
/// customer endpoint and applies authorization before returning any record.
/// </summary>
public sealed class ContractAuditQueryService : IContractAuditQueryService
{
    private const byte ActiveEmployeeStatus = 1;

    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public ContractAuditQueryService(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<ContractAuditResponse>> QueryAsync(
        ContractAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default)
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
            query = query.Where(audit => audit.ActorType == filter.ActorType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
        {
            query = query.Where(audit => audit.ActionType == filter.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Result))
        {
            query = query.Where(audit => audit.Result == filter.Result);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt <= filter.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = ((long)filter.Page - 1) * filter.PageSize;
        if (offset > int.MaxValue)
        {
            throw new ArgumentException("Requested audit page is outside the supported range.");
        }
        var records = await query
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.ContractAuditId)
            .Skip((int)offset)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var employeeActorIds = records
            .Where(audit => audit.ActorEmployeeId.HasValue)
            .Select(audit => audit.ActorEmployeeId!.Value)
            .Distinct()
            .ToList();
        var employeeActors = await _dbContext.TblEmployees
            .AsNoTracking()
            .Where(employee => employeeActorIds.Contains(employee.EmployeeId))
            .Select(employee => new
            {
                employee.EmployeeId,
                employee.EmployeeFullName,
                employee.EmployeeCode,
                employee.EmployeeAccount
            })
            .ToListAsync(cancellationToken);
        var employeeActorNames = employeeActors.ToDictionary(
            employee => employee.EmployeeId,
            employee => FirstNonEmpty(
                employee.EmployeeFullName,
                employee.EmployeeCode,
                employee.EmployeeAccount));

        var customerSessionIds = records
            .Where(audit => audit.ActorCustomerAccessSessionId.HasValue)
            .Select(audit => audit.ActorCustomerAccessSessionId!.Value)
            .Distinct()
            .ToList();
        var customerActors = await (
            from session in _dbContext.TblContractCustomerAccessSessions.AsNoTracking()
            join contract in _dbContext.TblContracts.AsNoTracking()
                on session.ContractId equals contract.ContractId
            join customer in _dbContext.TblCustomers.AsNoTracking()
                on contract.CustomerId equals customer.CustomerId
            where session.TenantId == tenantId
                && customerSessionIds.Contains(session.CustomerAccessSessionId)
            select new
            {
                session.CustomerAccessSessionId,
                customer.CustomerFullName,
                customer.CustomerCompany,
                customer.CustomerRepresentativeName,
                customer.CustomerCode
            })
            .ToListAsync(cancellationToken);
        var customerActorNames = customerActors.ToDictionary(
            customer => customer.CustomerAccessSessionId,
            customer => FirstNonEmpty(
                customer.CustomerFullName,
                customer.CustomerCompany,
                customer.CustomerRepresentativeName,
                customer.CustomerCode));

        return new PagedResult<ContractAuditResponse>
        {
            Items = records
                .Select(audit => Map(
                    audit,
                    employeeActorNames,
                    customerActorNames))
                .ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    private static ContractAuditResponse Map(
        Infrastructure.Persistence.Application.Models.TblContractAudit audit,
        IReadOnlyDictionary<int, string?> employeeActorNames,
        IReadOnlyDictionary<int, string?> customerActorNames) => new()
    {
        ContractAuditId = audit.ContractAuditId,
        ContractId = audit.ContractId,
        VersionId = audit.VersionId,
        SubjectType = audit.SubjectType ?? ContractAuditSubjectTypes.Contract,
        SubjectId = audit.SubjectId ?? audit.ContractId,
        ActorType = audit.ActorType,
        ActorEmployeeId = audit.ActorEmployeeId,
        ActorCustomerAccessSessionId = audit.ActorCustomerAccessSessionId,
        ActorDisplayName = ResolveActorDisplayName(
            audit,
            employeeActorNames,
            customerActorNames),
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

    private static string? ResolveActorDisplayName(
        Infrastructure.Persistence.Application.Models.TblContractAudit audit,
        IReadOnlyDictionary<int, string?> employeeActorNames,
        IReadOnlyDictionary<int, string?> customerActorNames)
    {
        if (audit.ActorEmployeeId.HasValue
            && employeeActorNames.TryGetValue(
                audit.ActorEmployeeId.Value,
                out var employeeName))
        {
            return employeeName;
        }

        if (audit.ActorCustomerAccessSessionId.HasValue
            && customerActorNames.TryGetValue(
                audit.ActorCustomerAccessSessionId.Value,
                out var customerName))
        {
            return customerName;
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static Dictionary<string, JsonElement>? ParseValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
    }

    private static void ValidateFilter(ContractAuditFilterRequest filter)
    {
        if (filter.ContractId is <= 0 || filter.VersionId is <= 0)
        {
            throw new ArgumentException("ContractId and VersionId must be positive.");
        }

        if (filter.Page <= 0 || filter.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("Page must be positive and PageSize must be from 1 to 100.");
        }

        if (filter.FromUtc.HasValue
                && filter.FromUtc.Value.Kind != DateTimeKind.Utc
            || filter.ToUtc.HasValue
                && filter.ToUtc.Value.Kind != DateTimeKind.Utc
            || filter.FromUtc > filter.ToUtc)
        {
            throw new ArgumentException("Audit time range must be valid UTC.");
        }

        ValidateValue(filter.ActorType, "ActorType", ActorTypes());
        ValidateValue(filter.Result, "Result", Results());
        ValidateValue(filter.ActionType, "ActionType", ActionTypes());
    }

    private static void ValidateValue(
        string? value,
        string name,
        HashSet<string> allowed)
    {
        if (!string.IsNullOrWhiteSpace(value) && !allowed.Contains(value))
        {
            throw new ArgumentException($"{name} is invalid.");
        }
    }

    private static HashSet<string> ActorTypes() =>
        [ContractAuditActorTypes.Employee, ContractAuditActorTypes.Customer,
            ContractAuditActorTypes.System];

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
}
