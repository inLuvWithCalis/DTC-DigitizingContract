using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class ContractPaymentMilestoneDateRequest
{
    [Range(1, int.MaxValue)]
    public int SourceTemplatePaymentMilestoneId { get; set; }

    public DateTime AnchorDate { get; set; }
}
