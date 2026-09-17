namespace ContractManagement.Infrastructure.Persistence.Application.Models;
public sealed class TblContractAcceptanceMilestone
{
    public int AcceptanceMilestoneId { get; set; }
    public int AcceptanceRecordId { get; set; }
    public int PaymentMilestoneId { get; set; }
}
