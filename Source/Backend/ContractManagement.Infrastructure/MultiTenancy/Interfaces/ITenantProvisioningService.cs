using ContractManagement.Infrastructure.MultiTenancy.Contracts;

namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

public interface ITenantProvisioningService
{
    Task<TenantProvisioningResult> CreateDedicatedAsync(
        TenantProvisioningCommand command,
        CancellationToken cancellationToken = default);
}