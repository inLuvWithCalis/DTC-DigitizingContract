namespace ContractManagement.API.Domains.Interfaces.Security;

public sealed record CentralSecurityAuditWriteRequest(
    int? ActorSystemAdminId,
    int? TenantId,
    string? TenantCode,
    string Action,
    string Result,
    string? TargetType,
    string? TargetId,
    string? FailureCode,
    byte? PreviousEmployeeType = null,
    byte? NewEmployeeType = null,
    byte? PreviousStatus = null,
    byte? NewStatus = null);

/// <summary>
/// Best-effort central audit writer. A failed audit is logged critically and
/// does not turn an already denied request into an allowed request.
/// </summary>
public interface ICentralSecurityAuditWriter
{
    Task TryWriteAsync(
        HttpContext httpContext,
        CentralSecurityAuditWriteRequest request,
        CancellationToken cancellationToken = default);
}
