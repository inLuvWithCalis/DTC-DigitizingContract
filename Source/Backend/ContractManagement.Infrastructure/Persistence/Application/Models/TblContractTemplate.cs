namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Metadata chung của một template.
///
/// Template có nhiều version, nhưng CurrentPublishedVersionId
/// chỉ trỏ đến version đang được dùng cho giao dịch mới.
/// </summary>
public partial class TblContractTemplate
{
    public int TemplateId { get; set; }

    /// <summary>
    /// Mã nghiệp vụ duy nhất, ví dụ HD_CUNG_CAP_PM_VI.
    /// </summary>
    public string TemplateCode { get; set; } = null!;

    public string TemplateName { get; set; } = null!;

    public string? TemplateNameEn { get; set; }

    /// <summary>
    /// Lưu giá trị TemplateDocumentType dưới dạng tinyint.
    /// </summary>
    public byte DocumentType { get; set; }

    /// <summary>
    /// 1 = Vietnamese, 2 = Bilingual.
    /// </summary>
    public byte LanguageMode { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Version Published hiện được chọn cho văn bản mới.
    /// Null khi template chưa publish version nào.
    /// </summary>
    public int? CurrentPublishedVersionId { get; set; }

    /// <summary>
    /// Template inactive không được chọn cho giao dịch mới.
    /// Dữ liệu lịch sử vẫn được giữ.
    /// </summary>
    public bool IsActive { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}