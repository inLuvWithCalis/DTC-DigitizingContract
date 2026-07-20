namespace ContractManagement.Common.Enums;

/// <summary>
/// Vòng đời của một phiên bản template.
/// </summary>
public enum TemplateVersionStatus : byte
{
    /// <summary>
    /// Đang thiết kế, được phép sửa DOCX, term và placeholder.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Đã kiểm tra và được phép dùng để tạo văn bản mới.
    /// Không được chỉnh sửa trực tiếp.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Không còn được chọn cho văn bản mới,
    /// nhưng vẫn phải lưu để tra cứu lịch sử.
    /// </summary>
    Retired = 2
}