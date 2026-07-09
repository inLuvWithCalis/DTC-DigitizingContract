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
        Task<PagedResult<EmployeeResponse>> GetListAsync(
            int page,
            int pageSize,
            string? keyword,
            byte? status,
            DateTime? dateCreated);

        Task<EmployeeResponse> GetByIdAsync(int id);

        Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request);

        Task UpdateAsync(int id, UpdateEmployeeRequest request);

        Task ChangePasswordAsync(int id, ChangePasswordRequest request);

        Task SetStatusAsync(int id, byte status);
    }
}