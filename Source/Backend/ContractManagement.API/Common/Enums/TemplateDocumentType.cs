namespace ContractManagement.Common.Enums;

/// <summary>
/// Loại văn bản được tạo từ template.
/// </summary>
public enum TemplateDocumentType : byte
{
    Quotation = 1,

    SoftwareSupplyContract = 2,

    PaymentRequest = 3,

    HandoverRecord = 4,

    AcceptanceRecord = 5,

    LiquidationRecord = 6,

    SoftwareMaintenanceContract = 7,

    SoftwareUpkeepContract = 8,

    /// <summary>
    /// Loại tài liệu hỗ trợ chưa được phân loại cụ thể.
    /// Không được dùng để tạo hợp đồng pháp lý mới.
    /// </summary>
    Other = 99
}