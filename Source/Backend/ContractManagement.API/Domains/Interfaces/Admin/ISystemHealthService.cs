using ContractManagement.API.Domains.DTOs.Responses.Admin;

namespace ContractManagement.API.Domains.Interfaces.Admin;

public interface ISystemHealthService
{
    Task<SystemHealthResponse> GetDetailedAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}
