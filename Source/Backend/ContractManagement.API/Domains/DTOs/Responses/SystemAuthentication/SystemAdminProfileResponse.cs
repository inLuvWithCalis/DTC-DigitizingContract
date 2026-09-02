namespace ContractManagement.API.Domains.DTOs.Responses.SystemAuthentication;

public sealed class SystemAdminProfileResponse
{
    public int SystemAdminId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string RoleName { get; set; } = "SystemAdmin";
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public string? ImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
