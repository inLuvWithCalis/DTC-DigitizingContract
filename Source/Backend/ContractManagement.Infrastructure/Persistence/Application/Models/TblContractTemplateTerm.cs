namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Điều khoản mềm thuộc một template version.
///
/// Khi tạo contract version, dữ liệu này sẽ được sao chép
/// sang TblContractTerm để hợp đồng có snapshot riêng.
/// </summary>
public partial class TblContractTemplateTerm
{
    public int TemplateTermId { get; set; }

    /// <summary>
    /// Template version sở hữu điều khoản.
    /// Đây là logical reference, không tạo foreign key vật lý.
    /// </summary>
    public int TemplateVersionId { get; set; }

    /// <summary>
    /// Mã ổn định của điều khoản.
    /// Ví dụ: PAYMENT, WARRANTY, CONFIDENTIALITY.
    /// </summary>
    public string TermCode { get; set; } = null!;

    /// <summary>
    /// Tiêu đề tiếng Việt.
    /// </summary>
    public string TermTitle { get; set; } = null!;

    /// <summary>
    /// Tiêu đề tiếng Anh cho template song ngữ.
    /// </summary>
    public string? TermTitleEn { get; set; }

    /// <summary>
    /// Nội dung điều khoản tiếng Việt.
    /// Draft có thể tạm thời để null.
    /// </summary>
    public string? TermContent { get; set; }

    /// <summary>
    /// Nội dung điều khoản tiếng Anh.
    /// Validation sẽ kiểm tra khi template là song ngữ.
    /// </summary>
    public string? TermContentEn { get; set; }

    /// <summary>
    /// Khách hàng có được gửi comment đàm phán
    /// cho điều khoản này hay không.
    /// </summary>
    public bool IsNegotiable { get; set; }

    /// <summary>
    /// Thứ tự điều khoản trong văn bản.
    /// </summary>
    public int DisplayOrder { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Concurrency token do SQL Server tự sinh.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}