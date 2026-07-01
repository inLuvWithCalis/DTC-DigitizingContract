using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Contracts.Tenants;

public sealed class CreateTenantRequest
{
    [Required]
    [RegularExpression(
        "^[a-z0-9-]{3,50}$",
        ErrorMessage =
            "TenantCode chỉ được chứa chữ thường, "
            + "số và dấu gạch ngang.")]
    public string TenantCode { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string TenantName { get; set; } = null!;
}