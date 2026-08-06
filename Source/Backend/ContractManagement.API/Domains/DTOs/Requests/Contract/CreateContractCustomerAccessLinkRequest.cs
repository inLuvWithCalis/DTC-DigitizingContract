using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class CreateContractCustomerAccessLinkRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
