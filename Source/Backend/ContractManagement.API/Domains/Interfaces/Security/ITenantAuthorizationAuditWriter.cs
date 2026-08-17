namespace ContractManagement.API.Domains.Interfaces.Security;

/// <summary>
/// Best-effort writer for denied tenant API requests. It never changes the
/// authorization outcome if audit persistence is unavailable.
/// </summary>
public interface ITenantAuthorizationAuditWriter
{
    Task TryWriteDeniedAsync(
        HttpContext httpContext,
        int? actorEmployeeId,
        string targetType,
        string? targetId,
        string failureCode,
        CancellationToken cancellationToken = default);
}
