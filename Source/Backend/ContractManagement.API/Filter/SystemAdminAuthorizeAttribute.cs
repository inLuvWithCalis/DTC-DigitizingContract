using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Filter;

/// <summary>
/// Re-validates the System Admin session against Central on every request and
/// records any denied Central API attempt without changing the denial result.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SystemAdminAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var systemAdminId = httpContext.Session.GetInt32("SystemAdminId");
        var centralDbContext = httpContext.RequestServices
            .GetRequiredService<CentralDbContext>();

        var isActive = systemAdminId.HasValue
            && await centralDbContext.SystemAdmins
                .AsNoTracking()
                .AnyAsync(admin => admin.SystemAdminId == systemAdminId.Value
                    && admin.IsActive,
                    httpContext.RequestAborted);

        if (isActive)
        {
            return;
        }

        await httpContext.RequestServices
            .GetRequiredService<ICentralSecurityAuditWriter>()
            .TryWriteAsync(
                httpContext,
                new CentralSecurityAuditWriteRequest(
                    systemAdminId,
                    null,
                    null,
                    AuthorizationAuditActionTypes.CentralApiAccessDenied,
                    AuthorizationAuditResultTypes.Denied,
                    "CentralApi",
                    GetTargetId(context),
                    AuthorizationErrorCodes.AuthenticationRequired),
                httpContext.RequestAborted);

        httpContext.Session.Remove("SystemAdminId");
        httpContext.Session.Remove("SystemAdminName");
        context.Result = new ObjectResult(new AuthorizationErrorResponse(
            AuthorizationErrorCodes.AuthenticationRequired,
            "System Admin login is required."))
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    private static string GetTargetId(AuthorizationFilterContext context)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controller = descriptor?.ControllerName ?? "Unknown";
        var action = descriptor?.ActionName ?? "Unknown";
        return $"{controller}/{action}";
    }
}
