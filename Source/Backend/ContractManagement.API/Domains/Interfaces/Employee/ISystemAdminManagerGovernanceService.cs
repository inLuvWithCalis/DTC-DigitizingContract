using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.DTOs.Responses.Employee;

namespace ContractManagement.Domains.Interfaces.Employee;

public interface ISystemAdminManagerGovernanceService
{
    Task<ManagerGovernanceResponse> ChangeManagerRoleAsync(
        int systemAdminId,
        string tenantCode,
        int employeeId,
        ChangeEmployeeRoleRequest request,
        CancellationToken cancellationToken = default);
}
