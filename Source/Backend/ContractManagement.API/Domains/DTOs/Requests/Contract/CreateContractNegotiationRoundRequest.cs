using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class CreateContractNegotiationRoundRequest
{
    [Range(1, int.MaxValue)]
    public int CurrentVersionId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    public string CurrentVersionRowVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string ChangeNote { get; set; } = string.Empty;
}
