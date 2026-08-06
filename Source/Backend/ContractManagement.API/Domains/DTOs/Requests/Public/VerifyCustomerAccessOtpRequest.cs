using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Public;

public sealed class VerifyCustomerAccessOtpRequest
{
    [Required]
    public string PublicChallengeId { get; set; } = string.Empty;

    [Required]
    public string Otp { get; set; } = string.Empty;
}
