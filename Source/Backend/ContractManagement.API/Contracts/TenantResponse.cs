using ContractManagement.Infrastructure.MultiTenancy.Enums;

namespace ContractManagement.Contracts.Tenants;

/// <summary>
/// Response không chứa ConnectionString.
/// </summary>
public sealed class TenantResponse
{
    public int TenantId { get; set; }

    public string TenantCode { get; set; } = null!;

    public string TenantName { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public TenantDatabaseMode DatabaseMode { get; set; }

    public TenantStatus Status { get; set; }
}