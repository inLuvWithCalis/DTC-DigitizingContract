using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.DTOs.Responses.Employee;
using ContractManagement.API.Common.Responses;

namespace ContractManagement.Domains.Interfaces.Employee
{
    /// <summary>
    /// Service quản lý nhân viên / tài khoản đăng nhập nội bộ.
    /// </summary>
    public interface IEmployeeService
    {
        Task<PagedResult<EmployeeResponse>> GetListAsync(EmployeeFilterRequest filter);

        Task<EmployeeResponse> GetByIdAsync(int id);

        Task<List<EmployeeDirectoryResponse>> GetDirectoryAsync();

        Task<PagedResult<EmployeeDirectoryResponse>> SearchDirectoryAsync(
            EmployeeDirectoryFilterRequest filter,
            CancellationToken cancellationToken = default);

        Task<EmployeeResponse> CreateManagedEmployeeAsync(
            int managerEmployeeId,
            CreateEmployeeRequest request,
            CancellationToken cancellationToken = default);

        Task UpdateManagedEmployeeAsync(
            int managerEmployeeId,
            int employeeId,
            UpdateEmployeeRequest request,
            CancellationToken cancellationToken = default);

        Task ResetManagedEmployeePasswordAsync(
            int managerEmployeeId,
            int employeeId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default);

        Task SetManagedEmployeeStatusAsync(
            int managerEmployeeId,
            int employeeId,
            SetEmployeeStatusRequest request,
            CancellationToken cancellationToken = default);
    }
}
