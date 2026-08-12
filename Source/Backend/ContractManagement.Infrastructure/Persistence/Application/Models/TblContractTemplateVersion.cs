namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Một phiên bản bất biến của template.
///
/// Draft được phép chỉnh sửa.
/// Published/Retired không được sửa nội dung hoặc thay DOCX.
/// </summary>
public partial class TblContractTemplateVersion
{
    public int TemplateVersionId { get; set; }

    public int TemplateId { get; set; }

    public int VersionNo { get; set; }

    public string? ChangeNote { get; set; }

    /// <summary>
    /// TemplateVersionStatus: Draft, Published hoặc Retired.
    /// </summary>
    public byte Status { get; set; }

    /// <summary>
    /// TemplateValidationStatus:
    /// NotValidated, Valid hoặc Invalid.
    /// </summary>
    public byte ValidationStatus { get; set; }

    /// <summary>
    /// Chi tiết lỗi placeholder/mapping khi validation thất bại.
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Logical reference tới tbl_FileStorage.FileId.
    /// </summary>
    public int? DocumentFileId { get; set; }

    /// <summary>
    /// SHA-256 hexadecimal gồm 64 ký tự của DOCX.
    /// </summary>
    public string? DocumentHash { get; set; }

    /// <summary>
    /// Logical reference tới tbl_FileStorage.FileId của DOCX preview hiện hành.
    /// Null kèm PreviewSourceHash còn lại nghĩa là preview đã stale và artifact cũ đã được dọn.
    /// </summary>
    public int? PreviewFileId { get; set; }

    /// <summary>
    /// Logical reference tới tbl_FileStorage.FileId của PDF preview đã publish.
    /// Artifact này bất biến và được giữ lại cả sau khi version retired.
    /// </summary>
    public int? PublishedPreviewPdfFileId { get; set; }

    /// <summary>
    /// SHA-256 của DocumentHash, catalog version, preview dataset version và LanguageMode.
    /// </summary>
    public string? PreviewSourceHash { get; set; }

    public DateTime? PreviewedAt { get; set; }

    public int? PreviewedByEmployeeId { get; set; }

    public int? ValidatedByEmployeeId { get; set; }

    public DateTime? ValidatedDate { get; set; }

    public int? PublishedByEmployeeId { get; set; }

    public DateTime? PublishedDate { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
