namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractPlaceholderDefinition
{
    public int PlaceholderDefinitionId { get; set; }
    public string PlaceholderKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string SourceFieldKey { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? FormatString { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedEmployeeId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedEmployeeId { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

// Separate from template audit: a tenant-wide definition has no template/version owner.
public sealed class TblContractPlaceholderAudit
{
    public long PlaceholderAuditId { get; set; }
    public string PlaceholderKey { get; set; } = string.Empty;
    public int ActorEmployeeId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? PreviousValuesJson { get; set; }
    public string NewValuesJson { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
