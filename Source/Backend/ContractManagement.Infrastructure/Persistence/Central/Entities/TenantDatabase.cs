using ContractManagement.Infrastructure.MultiTenancy.Enums;

namespace ContractManagement.Infrastructure.Persistence.Central.Entities;

/// <summary>
/// Đại diện cho một database chứa dữ liệu nghiệp vụ.
///
/// Giai đoạn Multiple Databases:
/// Một TenantDatabase thường chỉ có một Tenant.
///
/// Giai đoạn Hybrid:
/// Một TenantDatabase ở chế độ Shared có thể có nhiều Tenant.
/// </summary>
public sealed class TenantDatabase
{
    public int TenantDatabaseId { get; set; }

    /// <summary>
    /// Mã database do hệ thống quản lý.
    ///
    /// Ví dụ:
    /// dedicated-dtc
    /// shared-01
    /// </summary>
    public string DatabaseKey { get; set; } = null!;

    /// <summary>
    /// Tên database thật trên SQL Server.
    ///
    /// Ví dụ:
    /// ContractManagement_Tenant_dtc
    /// </summary>
    public string DatabaseName { get; set; } = null!;

    /// <summary>
    /// Connection string dùng để truy cập database.
    ///
    /// Tuyệt đối không trả giá trị này trong API response.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    public TenantDatabaseMode Mode { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Những tenant đang sử dụng database này.
    /// </summary>
    public ICollection<Tenant> Tenants { get; set; }
        = new List<Tenant>();
}