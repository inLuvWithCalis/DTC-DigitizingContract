using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractSigningArtifactResponse
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

public sealed class ContractSignedEvidenceResponse
{
    public int SignedEvidenceId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int VersionNo { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public SignedEvidenceStatus Status { get; set; }
    public string ProviderSignerName { get; set; } = string.Empty;
    public string ProviderSignerTitle { get; set; } = string.Empty;
    public DateTime ProviderSigningDate { get; set; }
    public string CustomerSignerName { get; set; } = string.Empty;
    public string CustomerSignerTitle { get; set; } = string.Empty;
    public DateTime CustomerSigningDate { get; set; }
    public int? SupersedesEvidenceId { get; set; }
    public string? SupersedeReason { get; set; }
    public int UploadedByEmployeeId { get; set; }
    public string? UploadedByEmployeeName { get; set; }
    public DateTime UploadedAt { get; set; }
    public int? SupersededByEmployeeId { get; set; }
    public string? SupersededByEmployeeName { get; set; }
    public DateTime? SupersededAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractSigningDetailResponse
{
    public int ContractId { get; set; }
    public ContractStatus ContractStatus { get; set; }
    public int VersionId { get; set; }
    public int VersionNo { get; set; }
    public bool VersionLocked { get; set; }
    public string ContractRowVersion { get; set; } = string.Empty;
    public string VersionRowVersion { get; set; } = string.Empty;
    public IReadOnlyList<ContractSigningArtifactResponse> ApprovedArtifacts { get; set; }
        = [];
    public ContractSignedEvidenceResponse? ActiveEvidence { get; set; }
    public IReadOnlyList<ContractSignedEvidenceResponse> EvidenceHistory { get; set; }
        = [];
}
