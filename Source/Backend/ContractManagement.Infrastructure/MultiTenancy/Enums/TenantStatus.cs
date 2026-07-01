namespace ContractManagement.Infrastructure.MultiTenancy.Enums;

/// <summary>
/// Trạng thái vòng đời của một tenant.
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant vừa được khai báo.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Hệ thống đang tạo database và schema.
    /// </summary>
    Provisioning = 2,

    /// <summary>
    /// Tenant đã sẵn sàng sử dụng.
    /// </summary>
    Active = 3,

    /// <summary>
    /// Quá trình tạo database gặp lỗi.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Tenant bị tạm khóa.
    /// </summary>
    Suspended = 5
}