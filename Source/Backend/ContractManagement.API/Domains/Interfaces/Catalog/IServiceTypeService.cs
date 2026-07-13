using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.Domains.DTOs.Responses.Catalog;

namespace ContractManagement.API.Domains.Interfaces.Catalog
{
    /// <summary>
    /// Service quản lý loại dịch vụ.
    /// </summary>
    public interface IServiceTypeService
    {
        Task<PagedResult<ServiceTypeResponse>> GetListAsync(ServiceTypeFilterRequest filter);

        Task<ServiceTypeResponse> GetByIdAsync(byte id);

        Task<ServiceTypeResponse> CreateAsync(CreateServiceTypeRequest request);

        Task UpdateAsync(byte id, UpdateServiceTypeRequest request);

        Task DeleteAsync(byte id);
    }
}
