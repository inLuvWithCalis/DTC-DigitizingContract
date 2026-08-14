using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ContractManagement.API.Common.Security;

/// <summary>
/// Identifies the RBAC-sensitive tenant API groups whose denied attempts must
/// be recorded in the tenant authorization audit.
/// </summary>
public static class SecurityAuditEndpointClassifier
{
    public static SecurityAuditTarget? GetTenantTarget(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint()?.Metadata
            .GetMetadata<SessionAuthorizeAttribute>() is null)
        {
            return null;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        var targetType = path.StartsWith("/api/contracts", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/contract-audits", StringComparison.OrdinalIgnoreCase)
            ? "Contract"
            : path.StartsWith("/api/contract-templates", StringComparison.OrdinalIgnoreCase)
                ? "Template"
                : path.StartsWith("/api/files", StringComparison.OrdinalIgnoreCase)
                    ? "File"
                    : path.StartsWith("/api/security-audits", StringComparison.OrdinalIgnoreCase)
                        ? "SecurityAudit"
                        : IsTenantManagementEndpoint(httpContext)
                            ? "Administration"
                            : null;

        if (targetType is null)
        {
            return null;
        }

        var routeValues = httpContext.Request.RouteValues
            .Where(value => !string.Equals(value.Key, "controller", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value.Key, "action", StringComparison.OrdinalIgnoreCase)
                && value.Value is not null)
            .Select(value => $"{value.Key}:{value.Value}");

        var targetId = string.Join(";", routeValues);
        return new SecurityAuditTarget(
            targetType,
            string.IsNullOrWhiteSpace(targetId) ? null : targetId);
    }

    private static bool IsTenantManagementEndpoint(HttpContext httpContext)
    {
        var descriptor = httpContext.GetEndpoint()?
            .Metadata.GetMetadata<ControllerActionDescriptor>();
        var controllerNamespace = descriptor?.ControllerTypeInfo.Namespace;

        return controllerNamespace is not null
               && controllerNamespace.Contains("Controllers.Admin", StringComparison.Ordinal);
    }
}

public sealed record SecurityAuditTarget(string TargetType, string? TargetId);
