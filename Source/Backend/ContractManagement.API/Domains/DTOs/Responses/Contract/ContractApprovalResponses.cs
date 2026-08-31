using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractApprovalArtifactResponse
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

public class ContractApprovalRequestResponse
{
    public int ApprovalRequestId { get; set; }
    public int ContractId { get; set; }
    public string? ContractCode { get; set; }
    public string ContractName { get; set; } = string.Empty;
    public int ResponsibleEmployeeId { get; set; }
    public string? ResponsibleEmployeeName { get; set; }
    public int VersionId { get; set; }
    public int VersionNo { get; set; }
    public string? SnapshotHash { get; set; }
    public ApprovalRequestStatus Status { get; set; }
    public int SubmittedByEmployeeId { get; set; }
    public string? SubmittedByEmployeeName { get; set; }
    public DateTime SubmittedDate { get; set; }
    public int? ResolvedByEmployeeId { get; set; }
    public string? ResolvedByEmployeeName { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? DecisionComment { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractApprovalDetailResponse
    : ContractApprovalRequestResponse
{
    public IReadOnlyList<ContractApprovalArtifactResponse> Artifacts { get; set; }
        = Array.Empty<ContractApprovalArtifactResponse>();
}

public sealed class ContractApprovalActionResponse
{
    public int ApprovalRequestId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public ApprovalRequestStatus ApprovalStatus { get; set; }
    public ContractStatus ContractStatus { get; set; }
    public int ResolvedByEmployeeId { get; set; }
    public DateTime ResolvedDate { get; set; }
    public string? DecisionComment { get; set; }
    public string ApprovalRequestRowVersion { get; set; } = string.Empty;
    public string ContractRowVersion { get; set; } = string.Empty;
}
