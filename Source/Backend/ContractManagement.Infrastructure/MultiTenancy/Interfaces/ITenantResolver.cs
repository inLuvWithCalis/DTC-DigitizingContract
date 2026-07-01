using ContractManagement.Infrastructure.MultiTenancy.Models;

namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

public interface ITenantResolver
{
    Task<ResolvedTenant?> ResolveAsync(
        string tenantCode,
        CancellationToken cancellationToken = default);
}