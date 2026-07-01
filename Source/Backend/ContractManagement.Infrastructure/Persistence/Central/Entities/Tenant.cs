using ContractManagement.Infrastructure.MultiTenancy.Enums;

namespace ContractManagement.Infrastructure.Persistence.Central.Entities;

/// <summary>
/// Đại diện cho một khách hàng hoặc công ty
/// đang sử dụng hệ thống.
/// </summary>
public sealed class Tenant
{
    /// <summary>
    /// ID nội bộ của tenant.
    ///
    /// Sau này trong Shared Database,
    /// giá trị này sẽ được lưu vào cột TenantId.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Mã tenant được dùng để định tuyến request.
    ///
    /// Ví dụ:
    /// dtc
    /// abc-company
    /// </summary>
    public string TenantCode { get; set; } = null!;

    public string TenantName { get; set; } = null!;

    public TenantStatus Status { get; set; }

    /// <summary>
    /// Database tenant đang sử dụng.
    /// </summary>
    public int TenantDatabaseId { get; set; }

    public TenantDatabase TenantDatabase { get; set; } = null!;

    /// <summary>
    /// Thông báo lỗi khi tạo database thất bại.
    /// </summary>
    public string? ProvisioningError { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}