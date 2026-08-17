using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.Interfaces.Security;

namespace ContractManagement.Middleware;

/// <summary>
/// Audits denied responses from tenant RBAC endpoints, including controller
/// actions that translate their own object-authorization failures to 403/404.
/// </summary>
public sealed class TenantDeniedAuthorizationAuditMiddleware
{
    private readonly RequestDelegate _next;

    public TenantDeniedAuthorizationAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var failureCode = context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => AuthorizationErrorCodes.AuthenticationRequired,
            StatusCodes.Status403Forbidden => AuthorizationErrorCodes.PermissionDenied,
            StatusCodes.Status404NotFound => AuthorizationErrorCodes.ResourceNotFound,
            _ => null
        };
        if (failureCode is null)
        {
            return;
        }

        var target = SecurityAuditEndpointClassifier.GetTenantTarget(context);
        if (target is null)
        {
            return;
        }

        var writer = context.RequestServices
            .GetRequiredService<ITenantAuthorizationAuditWriter>();
        await writer.TryWriteDeniedAsync(
            context,
            SecurityAuditHttpContextItems.GetDeniedActorEmployeeId(context),
            target.TargetType,
            target.TargetId,
            failureCode,
            context.RequestAborted);
    }
}
