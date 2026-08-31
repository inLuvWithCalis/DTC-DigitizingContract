using System.ComponentModel.DataAnnotations.Schema;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Một feedback/reply bất biến về một version cụ thể của Contract.
/// Chỉ State và UpdatedDate được phép thay đổi trong lifecycle.
/// </summary>
public partial class TblContractNegotiationComment
{
    public int CommentId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public int? TermId { get; set; }

    public int? ParentCommentId { get; set; }

    /// <summary>
    /// Comment ở version liền trước mà bản ghi này được carry-forward từ đó.
    /// Null với comment được tạo trực tiếp trong version hiện tại.
    /// </summary>
    public int? CarriedForwardFromCommentId { get; set; }

    /// <summary>
    /// Version nguồn của lần carry-forward gần nhất.
    /// Đây là logical reference để giữ provenance ngay cả khi chỉ đọc comment mới.
    /// </summary>
    public int? CarriedForwardFromVersionId { get; set; }

    public string Content { get; set; } = null!;

    /// <summary>
    /// Slice 05 chỉ ghi nhận nguồn ExternalFeedback.
    /// </summary>
    public string Source { get; set; } = "ExternalFeedback";

    public int? RecordedByEmployeeId { get; set; }

    public int? CustomerAccessSessionId { get; set; }

    /// <summary>
    /// 0 = Open, 1 = Resolved.
    /// </summary>
    public byte State { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    [NotMapped]
    public bool ExternalFeedback
    {
        get => string.Equals(
            Source,
            "ExternalFeedback",
            StringComparison.Ordinal);
        set
        {
            if (value)
            {
                Source = "ExternalFeedback";
            }
        }
    }

    [NotMapped]
    public bool IsResolved
    {
        get => State == 1;
        set => State = value ? (byte)1 : (byte)0;
    }

    [NotMapped]
    public int CreatedEmployeeId
    {
        get => RecordedByEmployeeId ?? 0;
        set => RecordedByEmployeeId = value;
    }
}
