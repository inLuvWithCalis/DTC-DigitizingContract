namespace ContractManagement.Common.Enums;

/// <summary>
/// Kết quả nghiệp vụ được tạo ra khi sử dụng template.
///
/// Enum này được policy suy ra từ TemplateDocumentType;
/// không cần lưu thành cột riêng trong database.
/// </summary>
public enum TemplateOutputKind : byte
{
    Contract = 1,

    SupportingDocument = 2
}