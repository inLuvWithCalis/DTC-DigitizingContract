using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Bổ sung request và tenant metadata cho Contract business audit.
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

        var audits = requests
            .Select(request =>
            {
                if (request.ActorEmployeeId <= 0)
                {
                    throw new InvalidOperationException(
                        "Employee audit actor phải có EmployeeId hợp lệ.");
                }

                return new TblContractAudit
                {
                    TenantId = tenantId,
                    ContractId = request.ContractId,
                    VersionId = request.VersionId,
                    ActorType = ContractAuditActorTypes.Employee,
                    ActorEmployeeId = request.ActorEmployeeId,
                    ActionType = request.ActionType,
                    Result = request.Result,
                    PreviousContractStatus =
                        request.PreviousContractStatus,
                    NewContractStatus = request.NewContractStatus,
                    PreviousResponsibleEmployeeId =
                        request.PreviousResponsibleEmployeeId,
                    NewResponsibleEmployeeId =
                        request.NewResponsibleEmployeeId,
                    Reason = request.Reason,
                    OccurredAt = request.OccurredAt,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CorrelationId = correlationId
                };
            })
            .ToList();

        _dbContext.TblContractAudits.AddRange(audits);
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
