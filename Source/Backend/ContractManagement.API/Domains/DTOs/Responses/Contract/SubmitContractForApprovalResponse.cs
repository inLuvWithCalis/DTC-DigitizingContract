using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public class SubmitContractForApprovalResponse
{
    public int ApprovalRequestId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public ContractStatus ContractStatus { get; set; }

    public ApprovalRequestStatus ApprovalStatus { get; set; }

    public DateTime SubmittedDate { get; set; }

    public string SnapshotHash { get; set; } = string.Empty;

    public string ContractRowVersion { get; set; } = string.Empty;

    public string VersionRowVersion { get; set; } = string.Empty;
}