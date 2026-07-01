using ContractManagement.Infrastructure.MultiTenancy.Models;

namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

/// <summary>
/// Lưu tenant hiện tại trong phạm vi một request.
/// </summary>
public interface ICurrentTenant
{
    bool IsResolved { get; }

    ResolvedTenant? Value { get; }

    void Set(ResolvedTenant tenant);

    ResolvedTenant GetRequiredTenant();
}