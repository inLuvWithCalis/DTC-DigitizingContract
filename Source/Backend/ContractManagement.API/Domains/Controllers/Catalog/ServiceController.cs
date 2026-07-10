using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Catalog
{
    /// <summary>
    /// API quản lý dịch vụ.
    /// Dịch vụ là master data cho báo giá, hợp đồng, đơn hàng.
    /// </summary>
    [Route("api/catalog/services")]
    [ApiController]
    [SessionAuthorize]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _service;

        public ServiceController(IServiceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] ServiceFilterRequest filter)
        {
            var result = await _service.GetListAsync(filter);

            return Ok(ApiResponse<PagedResult<ServiceResponse>>.Ok(
                result,
                "Lấy danh sách dịch vụ thành công."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return Ok(ApiResponse<ServiceResponse>.Ok(
                result,
                "Lấy chi tiết dịch vụ thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateServiceRequest request)
        {
            var employeeId = GetCurrentEmployeeId();

            var result = await _service.CreateAsync(request, employeeId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ServiceId },
                ApiResponse<ServiceResponse>.Ok(
                    result,
                    "Tạo dịch vụ thành công."));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateServiceRequest request)
        {
            var employeeId = GetCurrentEmployeeId();

            await _service.UpdateAsync(id, request, employeeId);

            return Ok(ApiResponse<object>.Ok(
                new { serviceId = id },
                "Cập nhật dịch vụ thành công."));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(
            int id,
            [FromQuery] byte status)
        {
            var employeeId = GetCurrentEmployeeId();

            await _service.SetStatusAsync(id, status, employeeId);

            return Ok(ApiResponse<object>.Ok(
                new { serviceId = id, status },
                "Cập nhật trạng thái dịch vụ thành công."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = GetCurrentEmployeeId();

            await _service.DeleteAsync(id, employeeId);

            return Ok(ApiResponse<object>.Ok(
                new { serviceId = id },
                "Xóa dịch vụ thành công."));
        }

        /// <summary>
        /// Lấy nhân viên hiện tại từ session.
        /// SessionAuthorize đã chặn request chưa đăng nhập,
        /// nhưng vẫn check lại để tránh null crash.
        /// </summary>
        private int GetCurrentEmployeeId()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            return employeeId.Value;
        }
    }
}