namespace ContractManagement.API.Common.Security;

public static class SecurityAuditHttpContextItems
{
    public const string DeniedActorEmployeeIdKey = "SecurityAudit.DeniedActorEmployeeId";

    public static int? GetDeniedActorEmployeeId(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(DeniedActorEmployeeIdKey, out var value)
        && value is int employeeId
            ? employeeId
            : httpContext.Session.GetInt32("EmployeeId");
}
