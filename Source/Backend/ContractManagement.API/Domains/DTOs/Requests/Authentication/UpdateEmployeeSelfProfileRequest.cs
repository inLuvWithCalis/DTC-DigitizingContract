using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Authentication;

public sealed class UpdateEmployeeSelfProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    [MaxLength(1)]
    public string? Gender { get; set; }

    [MaxLength(1)]
    public string? MaritalStatus { get; set; }

    [MaxLength(15)]
    public string? Mobile { get; set; }

    [MaxLength(15)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
