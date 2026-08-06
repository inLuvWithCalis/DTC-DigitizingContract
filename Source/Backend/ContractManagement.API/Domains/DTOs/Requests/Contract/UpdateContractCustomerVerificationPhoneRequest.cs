using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class UpdateContractCustomerVerificationPhoneRequest
{
    [Required]
    public string PhoneSource { get; set; } = string.Empty;

    public string? ManualPhoneNumber { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
