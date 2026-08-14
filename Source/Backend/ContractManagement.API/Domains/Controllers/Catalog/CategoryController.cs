using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Catalog
{
    /// <summary>
    /// API quản lý danh mục sản phẩm.
    /// </summary>
    [Route("api/catalog/categories")]
    [ApiController]
    [SessionAuthorize]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        [SessionAuthorize(RbacPermissions.CatalogRead)]
        public async Task<IActionResult> GetList(
            [FromQuery] CategoryFilterRequest filter)
        {
            var result = await _service.GetListAsync(filter);

            return Ok(ApiResponse<PagedResult<CategoryResponse>>.Ok(
                result,
                "Lấy danh sách danh mục thành công."));
        }

        /// <summary>
        /// Lấy danh sách danh mục cha có danh mục con.
        /// </summary>
        [HttpGet("parents")]
        [SessionAuthorize(RbacPermissions.CatalogRead)]
        public async Task<IActionResult> GetParents([FromQuery] CategoryFilterRequest filter)
        {
            var result = await _service.GetParentsAsync(filter);

            return Ok(ApiResponse<PagedResult<CategoryResponse>>.Ok(
                result,
                "Lấy danh sách danh mục cha thành công."));
        }

        [HttpGet("{id:int}")]
        [SessionAuthorize(RbacPermissions.CatalogRead)]
        public async Task<IActionResult> GetById(int id)
        {
            var categoryId = ValidateCategoryId(id);

            var result = await _service.GetByIdAsync(categoryId);

            return Ok(ApiResponse<CategoryResponse>.Ok(
                result,
                "Lấy chi tiết danh mục thành công."));
        }

        [HttpPost]
        [SessionAuthorize(RbacPermissions.CatalogManage)]
        public async Task<IActionResult> Create(
            [FromBody] CreateCategoryRequest request)
        {
            var result = await _service.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.CategoryId },
                ApiResponse<CategoryResponse>.Ok(
                    result,
                    "Tạo danh mục thành công."));
        }

        [HttpPut("{id:int}")]
        [SessionAuthorize(RbacPermissions.CatalogManage)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCategoryRequest request)
        {
            var categoryId = ValidateCategoryId(id);

            await _service.UpdateAsync(categoryId, request);

            return Ok(ApiResponse<object>.Ok(
                new { categoryId },
                "Cập nhật danh mục thành công."));
        }

        [HttpDelete("{id:int}")]
        [SessionAuthorize(RbacPermissions.CatalogManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var categoryId = ValidateCategoryId(id);

            await _service.DeleteAsync(categoryId);

            return Ok(ApiResponse<object>.Ok(
                new { categoryId },
                "Xóa danh mục thành công."));
        }

        /// <summary>
        /// Entity CategoryId là byte, nhưng route ASP.NET nên nhận int.
        /// Hàm này đảm bảo id nằm trong range byte.
        /// </summary>
        private static byte ValidateCategoryId(int id)
        {
            if (id < byte.MinValue || id > byte.MaxValue)
            {
                throw new ArgumentException("CategoryId không hợp lệ.");
            }

            return (byte)id;
        }
    }
}
