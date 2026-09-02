namespace ContractManagement.API.Domains.DTOs.Responses.Authentication;

public sealed record AuthMeResponse(
    int EmployeeId,
    string? Account,
    string? FullName,
    byte EmployeeType,
    string RoleName,
    int TenantId,
    string TenantCode,
    string TenantName,
    string PermissionVersion,
    IReadOnlyList<string> Permissions,
    bool MustChangePassword,
    DateTime? PasswordChangedAt,
    string? ImageUrl);
