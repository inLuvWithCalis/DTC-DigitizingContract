using ContractManagement.API.Domains.DTOs.Requests.Department;
using ContractManagement.API.Domains.DTOs.Responses.Department;

namespace ContractManagement.API.Domains.Interfaces.Department
{
    public interface IDepartmentService
    {
        Task<List<DepartmentResponse>> GetAllAsync();

        Task<DepartmentResponse> GetByIdAsync(short id);

        Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request);

        Task UpdateAsync(short id, UpdateDepartmentRequest request);

        Task SetStatusAsync(short id, byte status);
    }
}
