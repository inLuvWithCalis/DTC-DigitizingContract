namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Bằng chứng scan có đủ chữ ký hai bên của một ContractVersion đã duyệt.
/// </summary>
public sealed class TblContractSignedEvidence
{
    public int SignedEvidenceId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public int FileId { get; set; }

    public byte Status { get; set; }

    public string ProviderSignerName { get; set; } = null!;

    public string ProviderSignerTitle { get; set; } = null!;

    public DateTime ProviderSigningDate { get; set; }

    public string CustomerSignerName { get; set; } = null!;

    public string CustomerSignerTitle { get; set; } = null!;

    public DateTime CustomerSigningDate { get; set; }

    /// <summary>
    /// Evidence cũ mà record hiện tại thay thế. Null với lần upload đầu tiên.
    /// </summary>
    public int? SupersedesEvidenceId { get; set; }

    public string? SupersedeReason { get; set; }

    public int UploadedByEmployeeId { get; set; }

    public DateTime UploadedAt { get; set; }

    public int? SupersededByEmployeeId { get; set; }

    public DateTime? SupersededAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
