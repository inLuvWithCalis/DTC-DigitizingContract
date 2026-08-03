using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public class TransferContractResponsibilityRequest
{
    [Range(1, int.MaxValue)]
    public int NewResponsibleEmployeeId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
