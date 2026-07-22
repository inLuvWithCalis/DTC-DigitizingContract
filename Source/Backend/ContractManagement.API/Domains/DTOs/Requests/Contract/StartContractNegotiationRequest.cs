using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public class StartContractNegotiationRequest
{
    /// <summary>
    /// RowVersion mới nhất của Contract.
    /// </summary>
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}