namespace ContractManagement.API.Domains.DTOs.Responses.Authentication;

public sealed class EmployeeProfileResponse
{
    public int EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Account { get; set; }
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public short? TitleId { get; set; }
    public string? TitleName { get; set; }
    public byte? EmployeeType { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public byte? Status { get; set; }
    public string? ImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? DefaultPage { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
