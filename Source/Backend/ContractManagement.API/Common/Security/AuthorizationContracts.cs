using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Common.Security;

public static class AuthorizationErrorCodes
{
    public const string AuthenticationRequired = "AuthenticationRequired";
    public const string EmployeeInactive = "EmployeeInactive";
    public const string PermissionDenied = "PermissionDenied";
    public const string ResourceNotFound = "ResourceNotFound";
    public const string StaleRowVersion = "StaleRowVersion";
    public const string LastActiveManager = "LastActiveManager";
    public const string CurrentPasswordIncorrect = "CurrentPasswordIncorrect";
    public const string PasswordPolicyViolation = "PasswordPolicyViolation";
    public const string PasswordReuseNotAllowed = "PasswordReuseNotAllowed";
    public const string MustChangePassword = "MustChangePassword";
}

public sealed record AuthorizationErrorResponse(string Code, string Message);

public sealed class RbacOperationException : Exception
{
    public RbacOperationException(int statusCode, string code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    public string Code { get; }
}

public sealed record AuthenticatedEmployee(
    int EmployeeId,
    string? Account,
    string? FullName,
    EmployeeType EmployeeType,
    IReadOnlyList<string> Permissions,
    bool MustChangePassword = false,
    DateTime? PasswordChangedAt = null,
    string? ImageUrl = null);

public static class EmployeeAuthorizationContext
{
    public const string HttpContextItemKey = "ContractManagement.AuthenticatedEmployee";

    public static AuthenticatedEmployee? GetEmployee(HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(HttpContextItemKey, out var value)
            ? value as AuthenticatedEmployee
            : null;
    }
}
