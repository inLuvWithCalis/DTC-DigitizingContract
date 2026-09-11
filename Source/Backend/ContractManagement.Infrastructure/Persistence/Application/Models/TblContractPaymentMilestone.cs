namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractPaymentMilestone
{
    public int PaymentMilestoneId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int TermId { get; set; }
    public int? SourceTemplatePaymentMilestoneId { get; set; }
    public string MilestoneCode { get; set; } = null!;
    public string TitleVi { get; set; } = null!;
    public string? TitleEn { get; set; }
    public decimal PaymentPercent { get; set; }
    public byte DueAnchor { get; set; }
    public int DueOffsetDays { get; set; }
    public byte DayCountMode { get; set; }
    public string? ConditionVi { get; set; }
    public string? ConditionEn { get; set; }
    public int DisplayOrder { get; set; }
    public decimal Amount { get; set; }
    public DateTime? AnchorDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int CreatedEmployeeId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedEmployeeId { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
