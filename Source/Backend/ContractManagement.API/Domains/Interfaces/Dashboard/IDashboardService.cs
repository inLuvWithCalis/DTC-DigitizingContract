using ContractManagement.API.Domains.DTOs.Requests.Dashboard;
using ContractManagement.API.Domains.DTOs.Responses.Dashboard;

namespace ContractManagement.API.Domains.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(
        int employeeId,
        DashboardFilterRequest filter,
        CancellationToken cancellationToken = default);
}
