namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Đại diện cho một phiên bản bất biến của hợp đồng.
///
/// Một hợp đồng có thể có nhiều version, nhưng tbl_Contract.CurrentVersionId
/// chỉ trỏ đến version hiện hành.
/// </summary>
public partial class TblContractVersion
{
    /// <summary>
    /// Khóa chính của phiên bản.
    /// </summary>
    public int VersionId { get; set; }

    /// <summary>
    /// Hợp đồng sở hữu phiên bản này.
    /// Đây là logical reference, không tạo foreign key vật lý.
    /// </summary>
    public int ContractId { get; set; }

    /// <summary>
    /// Số phiên bản tăng dần trong phạm vi một hợp đồng:
    /// 1, 2, 3...
    /// </summary>
    public int VersionNo { get; set; }

    /// <summary>
    /// Phiên bản nguồn được sao chép để tạo ra phiên bản hiện tại.
    /// Version đầu tiên không có SourceVersionId.
    /// </summary>
    public int? SourceVersionId { get; set; }

    /// <summary>
    /// Phiên bản template được dùng để tạo version hợp đồng này.
    /// Phải lưu tại đây để lịch sử không thay đổi khi contract dùng template mới.
    /// </summary>
    public int? TemplateVersionId { get; set; }

    /// <summary>
    /// Lý do tạo phiên bản mới, ví dụ:
    /// "Điều chỉnh thời hạn bảo hành theo yêu cầu khách hàng".
    /// </summary>
    public string? ChangeNote { get; set; }

    /// <summary>
    /// Snapshot chuẩn hóa của toàn bộ nội dung pháp lý tại thời điểm khóa version.
    ///
    /// Dữ liệu dạng row như term/item vẫn được lưu ở các bảng riêng để sử dụng
    /// trên website. SnapshotJson là bản đóng băng phục vụ khôi phục, đối chiếu
    /// và chứng minh nội dung đã được duyệt/ký.
    /// </summary>
    public string? SnapshotJson { get; set; }

    /// <summary>
    /// SHA-256 dạng hexadecimal, gồm 64 ký tự, được tính từ SnapshotJson.
    /// Dùng để phát hiện nội dung của version bị thay đổi.
    /// </summary>
    public string? SnapshotHash { get; set; }

    /// <summary>
    /// Version đã khóa thì không được cập nhật nội dung trực tiếp.
    /// Muốn sửa phải tạo version mới.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Thời điểm version được khóa, sử dụng UTC.
    /// </summary>
    public DateTime? LockedDate { get; set; }

    /// <summary>
    /// Nhân viên thực hiện khóa version.
    /// </summary>
    public int? LockedByEmployeeId { get; set; }

    /// <summary>
    /// Nhân viên tạo version.
    /// </summary>
    public int CreatedEmployeeId { get; set; }

    /// <summary>
    /// Thời điểm tạo version, sử dụng UTC.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Concurrency token do SQL Server tự sinh.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}