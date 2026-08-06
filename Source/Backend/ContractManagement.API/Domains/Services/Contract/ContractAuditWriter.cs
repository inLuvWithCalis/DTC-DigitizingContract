using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Stages non-secret Contract audit facts for employee, customer, and system actors.
/// </summary>
public sealed class ContractAuditWriter : IContractAuditWriter
{
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxCorrelationIdLength = 100;

    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContractAuditWriter(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public void StageEmployeeAudits(
        IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        StageAudits(requests.Select(request => new ContractAuditWriteRequest(
            request.ContractId,
            request.VersionId,
            ContractAuditActorTypes.Employee,
            request.ActorEmployeeId,
            null,
            request.ActionType,
            request.Result,
            request.OccurredAt,
            request.PreviousContractStatus,
            request.NewContractStatus,
            request.PreviousResponsibleEmployeeId,
            request.NewResponsibleEmployeeId,
            request.Reason)).ToList());
    }

    public void StageAudits(
        IReadOnlyCollection<ContractAuditWriteRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            return;
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId = NormalizeAndLimit(
                httpContext?.TraceIdentifier,
                MaxCorrelationIdLength)
            ?? Guid.NewGuid().ToString("N");
        var ipAddress = NormalizeAndLimit(
            httpContext?.Connection.RemoteIpAddress?.ToString(),
            MaxIpAddressLength);
        var userAgent = NormalizeAndLimit(
            httpContext?.Request.Headers.UserAgent.ToString(),
            MaxUserAgentLength);

        var audits = requests.Select(request =>
        {
            ValidateActor(request);

            return new TblContractAudit
            {
                TenantId = tenantId,
                ContractId = request.ContractId,
                VersionId = request.VersionId,
                ActorType = request.ActorType,
                ActorEmployeeId = request.ActorEmployeeId,
                ActorCustomerAccessSessionId =
                    request.ActorCustomerAccessSessionId,
                ActionType = request.ActionType,
                Result = request.Result,
                PreviousContractStatus = request.PreviousContractStatus,
                NewContractStatus = request.NewContractStatus,
                PreviousResponsibleEmployeeId =
                    request.PreviousResponsibleEmployeeId,
                NewResponsibleEmployeeId = request.NewResponsibleEmployeeId,
                Reason = request.Reason,
                OccurredAt = request.OccurredAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = correlationId
            };
        }).ToList();

        _dbContext.TblContractAudits.AddRange(audits);
    }

    private static void ValidateActor(ContractAuditWriteRequest request)
    {
        var isEmployee = string.Equals(
            request.ActorType,
            ContractAuditActorTypes.Employee,
            StringComparison.Ordinal);
        var isCustomer = string.Equals(
            request.ActorType,
            ContractAuditActorTypes.Customer,
            StringComparison.Ordinal);
        var isSystem = string.Equals(
            request.ActorType,
            ContractAuditActorTypes.System,
            StringComparison.Ordinal);

        if ((!isEmployee && !isCustomer && !isSystem)
            || (isEmployee && (request.ActorEmployeeId is not > 0
                || request.ActorCustomerAccessSessionId.HasValue))
            || (isCustomer && (request.ActorEmployeeId.HasValue
                || request.ActorCustomerAccessSessionId is not > 0))
            || (isSystem && (request.ActorEmployeeId.HasValue
                || request.ActorCustomerAccessSessionId.HasValue)))
        {
            throw new InvalidOperationException("Contract audit actor is invalid.");
        }
    }

    private static string? NormalizeAndLimit(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
