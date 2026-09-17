namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractAcceptanceRecord
{
    public int AcceptanceRecordId { get; set; }
    public int ContractId { get; set; }
    public int ContractVersionId { get; set; }
    public int TemplateVersionId { get; set; }
    public string AcceptanceCode { get; set; } = null!;
    public DateTime AcceptanceDate { get; set; }
    public string Location { get; set; } = null!;
    public byte AcceptanceKind { get; set; }
    public byte Status { get; set; }
    public string? SnapshotHash { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByEmployeeId { get; set; }
    public DateTime? SignedAt { get; set; }
    public int? SignedByEmployeeId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledByEmployeeId { get; set; }
    public string? CancellationReason { get; set; }
    public int CreatedEmployeeId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedEmployeeId { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
