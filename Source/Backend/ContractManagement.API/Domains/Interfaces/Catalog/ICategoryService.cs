using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;

namespace ContractManagement.API.Domains.Interfaces.Catalog
{
    /// <summary>
    /// Service quản lý danh mục sản phẩm.
    /// </summary>
    public interface ICategoryService
    {
        Task<PagedResult<CategoryResponse>> GetListAsync(CategoryFilterRequest filter);

        /// <summary>
        /// Lấy danh sách danh mục cha có danh mục con.
        /// Dùng cho dropdown chọn danh mục cha trên Frontend.
        /// </summary>
        Task<PagedResult<CategoryResponse>> GetParentsAsync(CategoryFilterRequest filter);

        Task<CategoryResponse> GetByIdAsync(byte id);

        Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);

        Task UpdateAsync(byte id, UpdateCategoryRequest request);

        Task DeleteAsync(byte id);
    }
}