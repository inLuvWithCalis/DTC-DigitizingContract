using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.LegalProfiles;

public sealed class UpsertTenantLegalProfileRequest
{
    [Required]
    [MaxLength(500)]
    public string LegalEntityName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TaxCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RepresentativeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RepresentativeTitle { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(30)]
    public string? FaxNumber { get; set; }

    [MaxLength(100)]
    public string? BankAccountNumber { get; set; }

    [MaxLength(500)]
    public string? BankName { get; set; }

    /// <summary>
    /// Bỏ trống ở lần tạo đầu tiên; bắt buộc và phải khớp khi cập nhật.
    /// </summary>
    public string? RowVersion { get; set; }
}
