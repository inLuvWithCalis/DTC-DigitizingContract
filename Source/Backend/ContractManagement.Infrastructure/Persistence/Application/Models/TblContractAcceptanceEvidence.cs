namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Biên bản nghiệm thu scan gắn với ContractVersion đã ký.
/// </summary>
public sealed class TblContractAcceptanceEvidence
{
    public int AcceptanceEvidenceId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int FileId { get; set; }
    public int UploadedByEmployeeId { get; set; }
    public DateTime UploadedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
