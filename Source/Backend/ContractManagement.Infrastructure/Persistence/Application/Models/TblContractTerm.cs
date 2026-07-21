using System;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Điều khoản snapshot thuộc một phiên bản hợp đồng cụ thể.
///
/// Khi tạo version mới, hệ thống sao chép các term của version cũ
/// thành những row mới, không sửa ngược term thuộc version đã khóa.
/// </summary>
public partial class TblContractTerm
{
    /// <summary>
    /// Khóa chính của term.
    /// </summary>
    public int TermId { get; set; }

    /// <summary>
    /// Hợp đồng sở hữu term.
    /// Được giữ lại để phục vụ truy vấn và row-level authorization.
    /// </summary>
    public int ContractId { get; set; }

    /// <summary>
    /// Phiên bản hợp đồng chứa term này.
    /// Hai version khác nhau phải có các row term riêng biệt.
    /// </summary>
    public int VersionId { get; set; }

    /// <summary>
    /// Term nguồn trong template, nếu term được sao chép từ template.
    /// Term nhập thủ công có thể để null.
    /// </summary>
    public int? SourceTemplateTermId { get; set; }

    /// <summary>
    /// Mã ổn định của điều khoản, ví dụ:
    /// PAYMENT, WARRANTY, CONFIDENTIALITY.
    ///
    /// TermCode dùng để mapping placeholder Word và nhận diện
    /// cùng một điều khoản giữa nhiều version.
    /// </summary>
    public string TermCode { get; set; } = null!;

    /// <summary>
    /// Tiêu đề tiếng Việt.
    /// </summary>
    public string TermTitle { get; set; } = null!;

    /// <summary>
    /// Tiêu đề tiếng Anh, dùng cho hợp đồng song ngữ.
    /// </summary>
    public string? TermTitleEn { get; set; }

    /// <summary>
    /// Nội dung tiếng Việt.
    /// </summary>
    public string? TermContent { get; set; }

    /// <summary>
    /// Nội dung tiếng Anh, dùng cho hợp đồng song ngữ.
    /// </summary>
    public string? TermContentEn { get; set; }

    /// <summary>
    /// Khách hàng có được gửi comment đàm phán cho term này hay không.
    ///
    /// false không có nghĩa nhân viên được tự do sửa;
    /// quyền sửa của nhân viên vẫn phụ thuộc ContractStatus và version lock.
    /// </summary>
    public bool IsNegotiable { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong hợp đồng.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Nhân viên tạo row term.
    /// </summary>
    public int CreatedEmployeeId { get; set; }

    /// <summary>
    /// Thời điểm tạo, sử dụng UTC.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Nhân viên cập nhật gần nhất khi version vẫn chưa khóa.
    /// </summary>
    public int? UpdatedEmployeeId { get; set; }

    /// <summary>
    /// Thời điểm cập nhật gần nhất, sử dụng UTC.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Concurrency token do SQL Server tự sinh.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}