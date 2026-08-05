using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Public;

public sealed class RequestCustomerAccessOtpRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
}
