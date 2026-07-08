using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Customer;
using ContractManagement.API.Domains.DTOs.Responses.Customer;
using ContractManagement.API.Domains.Interfaces.Customer;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.CRM
{
    /// <summary>
    /// API quản lý khách hàng.
    /// Khách hàng sẽ được dùng khi tạo báo giá và hợp đồng.
    /// </summary>
    [Route("api/customers")]
    [ApiController]
    [SessionAuthorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách khách hàng có phân trang, tìm kiếm, lọc trạng thái.
        /// For example:
        /// GET /api/customers?page=1&pageSize=20&keyword=abc&status=1
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] CustomerFilterRequest filter)
        {
            var result = await _service.GetListAsync(filter);

            return Ok(
                ApiResponse<PagedResult<CustomerResponse>>.Ok(
                    result,
                    "Lấy danh sách khách hàng thành công."));
        }

        /// <summary>
        /// Lấy chi tiết một khách hàng.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return Ok(
                ApiResponse<CustomerResponse>.Ok(
                    result,
                    "Lấy chi tiết khách hàng thành công."));
        }

        /// <summary>
        /// Tạo khách hàng mới.
        /// Người tạo lấy từ Session EmployeeId.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCustomerRequest request)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _service.CreateAsync(
                request,
                employeeId.Value);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.CustomerId },
                ApiResponse<CustomerResponse>.Ok(
                    result,
                    "Tạo khách hàng thành công."));
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCustomerRequest request)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            await _service.UpdateAsync(
                id,
                request,
                employeeId.Value);

            return Ok(
                ApiResponse<object>.Ok(
                    new { customerId = id },
                    "Cập nhật khách hàng thành công."));
        }

        /// <summary>
        /// Bật/tắt trạng thái khách hàng.
        /// Quy ước:
        /// 1 = Active
        /// 0 = Inactive
        /// </summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(
            int id,
            [FromQuery] byte status)
        {
            await _service.SetStatusAsync(id, status);

            return Ok(
                ApiResponse<object>.Ok(
                    new { customerId = id, status },
                    "Cập nhật trạng thái khách hàng thành công."));
        }
    }
}