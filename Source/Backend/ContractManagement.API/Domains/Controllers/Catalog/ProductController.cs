using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Catalog
{
    /// <summary>
    /// API quản lý sản phẩm.
    /// Sản phẩm là dữ liệu nền cho báo giá, đơn hàng và hợp đồng.
    /// </summary>
    [Route("api/catalog/products")]
    [ApiController]
    [SessionAuthorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] ProductFilterRequest filter)
        {
            var result = await _service.GetListAsync(filter);

            return Ok(ApiResponse<PagedResult<ProductResponse>>.Ok(
                result,
                "Lấy danh sách sản phẩm thành công."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return Ok(ApiResponse<ProductResponse>.Ok(
                result,
                "Lấy chi tiết sản phẩm thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductRequest request)
        {
            var result = await _service.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ProductId },
                ApiResponse<ProductResponse>.Ok(
                    result,
                    "Tạo sản phẩm thành công."));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateProductRequest request)
        {
            await _service.UpdateAsync(id, request);

            return Ok(ApiResponse<object>.Ok(
                new { productId = id },
                "Cập nhật sản phẩm thành công."));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(
            int id,
            [FromQuery] byte status)
        {
            await _service.SetStatusAsync(id, status);

            return Ok(ApiResponse<object>.Ok(
                new { productId = id, status },
                "Cập nhật trạng thái sản phẩm thành công."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);

            return Ok(ApiResponse<object>.Ok(
                new { productId = id },
                "Xóa sản phẩm thành công."));
        }
    }
}