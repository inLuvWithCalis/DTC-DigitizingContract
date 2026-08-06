using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class RevokeContractCustomerAccessLinkRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    public string Reason { get; set; } = string.Empty;
}
