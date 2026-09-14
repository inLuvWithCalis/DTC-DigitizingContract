namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractVersionLegalSnapshot
{
    public long ContractVersionLegalSnapshotId { get; set; }
    public int VersionId { get; set; }
    public int ContractId { get; set; }
    public int VersionNo { get; set; }
    public int? SourceVersionId { get; set; }
    public int TemplateVersionId { get; set; }
    public string ContractCode { get; set; } = null!;
    public string ContractName { get; set; } = null!;
    public string? ContractNameEn { get; set; }
    public byte ContractType { get; set; }
    public DateTime ContractCreatedDate { get; set; }
    public DateTime? SignDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public byte LanguageMode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CreatedByEmployeeId { get; set; }

    public TblContractVersion Version { get; set; } = null!;
    public ICollection<TblContractVersionPartySnapshot> Parties { get; set; } = [];
    public ICollection<TblContractVersionPaymentMilestoneSnapshot> PaymentMilestones { get; set; } = [];
}

public sealed class TblContractVersionPartySnapshot
{
    public long ContractVersionPartySnapshotId { get; set; }
    public long ContractVersionLegalSnapshotId { get; set; }
    public byte PartyRole { get; set; }
    public int? SourceCustomerId { get; set; }
    public string LegalName { get; set; } = null!;
    public string? TaxCode { get; set; }
    public string Address { get; set; } = null!;
    public string RepresentativeName { get; set; } = null!;
    public string RepresentativeTitle { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }

    public TblContractVersionLegalSnapshot LegalSnapshot { get; set; } = null!;
}

public sealed class TblContractVersionPaymentMilestoneSnapshot
{
    public long ContractVersionPaymentMilestoneSnapshotId { get; set; }
    public long ContractVersionLegalSnapshotId { get; set; }
    public int SourcePaymentMilestoneId { get; set; }
    public int SourceTermId { get; set; }
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
    public byte PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? PaidByEmployeeId { get; set; }

    public TblContractVersionLegalSnapshot LegalSnapshot { get; set; } = null!;
}
