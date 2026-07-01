namespace ContractManagement.Infrastructure.MultiTenancy.Enums;

/// <summary>
/// Kiểu database mà tenant đang sử dụng.
/// </summary>
public enum TenantDatabaseMode
{
    /// <summary>
    /// Tenant có database riêng.
    /// Mỗi tenant sẽ có một database riêng biệt, không chia sẻ với tenant khác.
    /// </summary>
    Dedicated = 1,

    /// <summary>
    /// Nhiều tenant dùng chung một database.
    /// Hybrid mode, nhiều tenant sẽ chia sẻ cùng một database nhưng có thể có schema riêng cho từng tenant.
    /// </summary>
    Shared = 2
}