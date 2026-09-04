using ContractManagement.API.Domains.DTOs.Requests.AdminDashboard;
using ContractManagement.API.Domains.DTOs.Responses.AdminDashboard;

namespace ContractManagement.API.Domains.Interfaces.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetAsync(
        AdminDashboardFilterRequest filter,
        CancellationToken cancellationToken = default);
}
