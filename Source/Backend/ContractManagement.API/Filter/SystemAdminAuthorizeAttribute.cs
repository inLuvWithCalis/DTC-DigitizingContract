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
    public bool AllowWhenPasswordChangeRequired { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var systemAdminId = httpContext.Session.GetInt32(
            AccountSessionKeys.SystemAdminId);
        var sessionVersion = httpContext.Session.GetInt32(
            AccountSessionKeys.SystemAdminSessionVersion);
        var centralDbContext = httpContext.RequestServices
            .GetRequiredService<CentralDbContext>();

        var admin = systemAdminId.HasValue && sessionVersion.HasValue
            ? await centralDbContext.SystemAdmins
                .AsNoTracking()
                .Where(candidate => candidate.SystemAdminId == systemAdminId.Value)
                .Select(candidate => new
                {
                    candidate.IsActive,
                    candidate.MustChangePassword,
                    candidate.SessionVersion
                })
                .FirstOrDefaultAsync(
                    httpContext.RequestAborted)
            : null;

        var hasValidSession = admin is not null
            && admin.IsActive
            && admin.SessionVersion == sessionVersion;
        if (hasValidSession
            && (!admin!.MustChangePassword
                || AllowWhenPasswordChangeRequired))
        {
            return;
        }

        var failureCode = hasValidSession
            ? AuthorizationErrorCodes.MustChangePassword
            : AuthorizationErrorCodes.AuthenticationRequired;

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
                    failureCode),
                httpContext.RequestAborted);

        if (!hasValidSession)
        {
            httpContext.Session.Remove(AccountSessionKeys.SystemAdminId);
            httpContext.Session.Remove(AccountSessionKeys.SystemAdminName);
            httpContext.Session.Remove(
                AccountSessionKeys.SystemAdminSessionVersion);
        }
        context.Result = new ObjectResult(new AuthorizationErrorResponse(
            failureCode,
            hasValidSession
                ? "Bạn phải đổi mật khẩu trước khi tiếp tục."
                : "System Admin login is required."))
        {
            StatusCode = hasValidSession
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized
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
