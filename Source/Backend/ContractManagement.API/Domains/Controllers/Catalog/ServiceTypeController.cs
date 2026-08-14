using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Domains.DTOs.Responses.Catalog;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Catalog
{
    /// <summary>
    /// API quản lý loại dịch vụ.
    /// ServiceType là master data dùng để phân nhóm Service.
    /// </summary>
    [Route("api/catalog/service-types")]
    [ApiController]
    [SessionAuthorize]
    public class ServiceTypeController : ControllerBase
    {
        private readonly IServiceTypeService _service;

        public ServiceTypeController(IServiceTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        [SessionAuthorize(RbacPermissions.CatalogRead)]
        public async Task<IActionResult> GetList(
            [FromQuery] ServiceTypeFilterRequest filter)
        {
            var result = await _service.GetListAsync(filter);

            return Ok(ApiResponse<PagedResult<ServiceTypeResponse>>.Ok(
                result,
                "Lấy danh sách loại dịch vụ thành công."));
        }

        [HttpGet("{id:int}")]
        [SessionAuthorize(RbacPermissions.CatalogRead)]
        public async Task<IActionResult> GetById(int id)
        {
            var serviceTypeId = ValidateServiceTypeId(id);

            var result = await _service.GetByIdAsync(serviceTypeId);

            return Ok(ApiResponse<ServiceTypeResponse>.Ok(
                result,
                "Lấy chi tiết loại dịch vụ thành công."));
        }

        [HttpPost]
        [SessionAuthorize(RbacPermissions.CatalogManage)]
        public async Task<IActionResult> Create(
            [FromBody] CreateServiceTypeRequest request)
        {
            var result = await _service.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ServiceTypeId },
                ApiResponse<ServiceTypeResponse>.Ok(
                    result,
                    "Tạo loại dịch vụ thành công."));
        }

        [HttpPut("{id:int}")]
        [SessionAuthorize(RbacPermissions.CatalogManage)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateServiceTypeRequest request)
        {
            var serviceTypeId = ValidateServiceTypeId(id);

            await _service.UpdateAsync(serviceTypeId, request);

            return Ok(ApiResponse<object>.Ok(
                new { serviceTypeId },
                "Cập nhật loại dịch vụ thành công."));
        }

        [HttpDelete("{id:int}")]
        [SessionAuthorize(RbacPermissions.CatalogManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var serviceTypeId = ValidateServiceTypeId(id);

            await _service.DeleteAsync(serviceTypeId);

            return Ok(ApiResponse<object>.Ok(
                new { serviceTypeId },
                "Xóa loại dịch vụ thành công."));
        }

        /// <summary>
        /// ServiceTypeId trong entity là byte.
        /// Route vẫn nhận int để tránh dùng route constraint :byte/:short không ổn định.
        /// </summary>
        private static byte ValidateServiceTypeId(int id)
        {
            if (id < byte.MinValue || id > byte.MaxValue)
            {
                throw new ArgumentException("ServiceTypeId không hợp lệ.");
            }

            return (byte)id;
        }
    }
}
