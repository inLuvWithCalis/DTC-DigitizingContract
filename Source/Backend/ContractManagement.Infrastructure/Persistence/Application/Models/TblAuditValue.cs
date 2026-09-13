namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public enum AuditValueSide : byte
{
    Previous = 1,
    New = 2
}

public enum AuditScalarValueKind : byte
{
    Integer = 1,
    Decimal = 2,
    String = 3,
    DateTime = 4,
    Boolean = 5
}

public sealed class TblContractAuditValue
{
    public long ContractAuditValueId { get; set; }

    public int ContractAuditId { get; set; }

    public AuditValueSide ValueSide { get; set; }

    public short FieldCode { get; set; }

    public AuditScalarValueKind ValueKind { get; set; }

    public bool IsNull { get; set; }

    public long? IntegerValue { get; set; }

    public decimal? DecimalValue { get; set; }

    public string? StringValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public bool? BooleanValue { get; set; }

    public TblContractAudit ContractAudit { get; set; } = null!;
}

public sealed class TblContractTemplateAuditValue
{
    public long ContractTemplateAuditValueId { get; set; }

    public int ContractTemplateAuditId { get; set; }

    public AuditValueSide ValueSide { get; set; }

    public byte FieldCode { get; set; }

    public bool IsNull { get; set; }

    public int? IntegerValue { get; set; }

    public long? LongValue { get; set; }

    public string? StringValue { get; set; }

    public TblContractTemplateAudit ContractTemplateAudit { get; set; } = null!;
}
