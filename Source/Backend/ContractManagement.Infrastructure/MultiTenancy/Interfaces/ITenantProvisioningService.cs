using ContractManagement.Infrastructure.MultiTenancy.Contracts;

namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

public interface ITenantProvisioningService
{
    Task<IReadOnlyList<TenantProvisioningResult>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<TenantProvisioningResult> CreateDedicatedAsync(
        TenantProvisioningCommand command,
        CancellationToken cancellationToken = default);
}
