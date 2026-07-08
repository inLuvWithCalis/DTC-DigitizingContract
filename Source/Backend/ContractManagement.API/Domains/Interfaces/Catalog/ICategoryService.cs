using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;

namespace ContractManagement.API.Domains.Interfaces.Catalog
{
    /// <summary>
    /// Service quản lý danh mục sản phẩm.
    /// </summary>
    public interface ICategoryService
    {
        Task<List<CategoryResponse>> GetAllAsync();

        Task<CategoryResponse> GetByIdAsync(byte id);

        Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);

        Task UpdateAsync(byte id, UpdateCategoryRequest request);

        Task DeleteAsync(byte id);
    }
}