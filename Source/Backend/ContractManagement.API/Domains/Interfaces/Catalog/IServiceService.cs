using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;

namespace ContractManagement.API.Domains.Interfaces.Catalog
{
    /// <summary>
    /// Service quản lý dịch vụ.
    /// </summary>
    public interface IServiceService
    {
        Task<PagedResult<ServiceResponse>> GetListAsync(ServiceFilterRequest filter);

        Task<ServiceResponse> GetByIdAsync(int id);

        Task<ServiceResponse> CreateAsync(CreateServiceRequest request, int createdBy);

        Task UpdateAsync(int id, UpdateServiceRequest request, int updatedBy);

        Task SetStatusAsync(int id, byte status, int updatedBy);

        Task DeleteAsync(int id, int updatedBy);
    }
}