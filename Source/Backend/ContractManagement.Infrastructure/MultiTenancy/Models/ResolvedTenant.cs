using ContractManagement.Infrastructure.MultiTenancy.Enums;

namespace ContractManagement.Infrastructure.MultiTenancy.Models;

/// <summary>
/// Thông tin tenant đang xử lý trong request hiện tại.
///
/// Đây không phải entity database.
/// </summary>
public sealed record ResolvedTenant(
    int TenantId,
    string TenantCode,
    string TenantName,
    TenantDatabaseMode DatabaseMode,
    string ConnectionString
);