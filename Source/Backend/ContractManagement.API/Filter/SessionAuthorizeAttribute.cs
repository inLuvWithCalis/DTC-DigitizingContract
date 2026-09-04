using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Filter;

/// <summary>
/// Authorizes an employee session against fresh tenant-database state.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _requiredPermissions;

    public SessionAuthorizeAttribute(params string[] requiredPermissions)
    {
        _requiredPermissions = requiredPermissions ?? Array.Empty<string>();
    }

    public bool AllowWhenPasswordChangeRequired { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var session = httpContext.Session;
        var employeeId = session.GetInt32(AccountSessionKeys.EmployeeId);
        var sessionVersion = session.GetInt32(
            AccountSessionKeys.EmployeeSessionVersion);
        var tenantId = session.GetInt32("TenantId");

        if (!employeeId.HasValue
            || !sessionVersion.HasValue
            || !tenantId.HasValue)
        {
            context.Result = Error(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee login is required.");
            return;
        }

        var currentTenant = httpContext.RequestServices
            .GetRequiredService<ICurrentTenant>();

        if (!currentTenant.IsResolved
            || currentTenant.Value!.TenantId != tenantId.Value)
        {
            PreserveDeniedAuditActor(httpContext, employeeId.Value);
            session.Clear();
            context.Result = Error(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee session is not valid for the current tenant.");
            return;
        }

        var dbContext = httpContext.RequestServices
            .GetRequiredService<DbDtctechContext>();

        var employee = await dbContext.TblEmployees
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId.Value)
            .Select(x => new
            {
                x.EmployeeId,
                Account = x.EmployeeAccount,
                FullName = x.EmployeeFullName,
                x.EmployeeType,
                x.Status,
                x.MustChangePassword,
                x.SessionVersion,
                x.PasswordChangedAt,
                x.DefaultPage,
                x.AvatarStorageKey,
                x.AvatarUpdatedAt
            })
            .FirstOrDefaultAsync(httpContext.RequestAborted);

        if (employee is null)
        {
            PreserveDeniedAuditActor(httpContext, employeeId.Value);
            session.Clear();
            context.Result = Error(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee session is no longer valid.");
            return;
        }

        if (employee.Status != 1)
        {
            PreserveDeniedAuditActor(httpContext, employee.EmployeeId);
            session.Clear();
            context.Result = Error(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.EmployeeInactive,
                "Employee account is inactive.");
            return;
        }

        if (employee.SessionVersion != sessionVersion.Value)
        {
            PreserveDeniedAuditActor(httpContext, employee.EmployeeId);
            session.Clear();
            context.Result = Error(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee session has expired.");
            return;
        }

        if (!EmployeePermissionCatalog.TryGetPermissions(
                employee.EmployeeType,
                out var permissions))
        {
            context.Result = Error(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Employee role is not valid for RBAC v1.");
            return;
        }

        if (employee.MustChangePassword
            && !AllowWhenPasswordChangeRequired)
        {
            context.Result = Error(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.MustChangePassword,
                "Bạn phải đổi mật khẩu trước khi tiếp tục.");
            return;
        }

        if (_requiredPermissions.Any(required =>
                !permissions.Contains(required, StringComparer.Ordinal)))
        {
            context.Result = Error(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Employee does not have the required permission.");
            return;
        }

        httpContext.Items[EmployeeAuthorizationContext.HttpContextItemKey] =
            new AuthenticatedEmployee(
                employee.EmployeeId,
                employee.Account,
                employee.FullName,
                (EmployeeType)employee.EmployeeType!.Value,
                permissions,
                employee.MustChangePassword,
                employee.PasswordChangedAt,
                employee.AvatarStorageKey is null
                    ? null
                    : $"/api/auth/profile/avatar?v={employee.AvatarUpdatedAt?.Ticks ?? 0}",
                employee.DefaultPage);
    }

    private static ObjectResult Error(int statusCode, string code, string message)
    {
        return new ObjectResult(new AuthorizationErrorResponse(code, message))
        {
            StatusCode = statusCode
        };
    }

    private static void PreserveDeniedAuditActor(HttpContext httpContext, int employeeId)
    {
        httpContext.Items[SecurityAuditHttpContextItems.DeniedActorEmployeeIdKey] =
            employeeId;
    }

}
