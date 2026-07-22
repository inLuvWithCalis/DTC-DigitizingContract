namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Một lần gửi hợp đồng đi xét duyệt.
///
/// Nếu bị Returned, người dùng sửa hợp đồng rồi tạo request mới.
/// Không tái sử dụng request cũ.
/// </summary>
public class TblContractApprovalRequest
{
    public int ApprovalRequestId { get; set; }

    public int ContractId { get; set; }

    /// <summary>
    /// Version được gửi duyệt.
    /// Version này phải được khóa.
    /// </summary>
    public int VersionId { get; set; }

    /// <summary>
    /// Workflow dùng để xét duyệt.
    /// Có thể null trong giai đoạn chưa cấu hình workflow.
    /// </summary>
    public int? WorkflowId { get; set; }

    /// <summary>
    /// Mapping ApprovalRequestStatus:
    /// 0 Pending, 1 Approved, 2 Returned,
    /// 3 Rejected, 4 Withdrawn.
    /// </summary>
    public byte Status { get; set; }

    public int SubmittedByEmployeeId { get; set; }

    public DateTime SubmittedDate { get; set; }

    public int? ResolvedByEmployeeId { get; set; }

    public DateTime? ResolvedDate { get; set; }

    public string? DecisionComment { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}