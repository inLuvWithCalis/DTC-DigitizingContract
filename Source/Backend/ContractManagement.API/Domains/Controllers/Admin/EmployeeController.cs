using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.DTOs.Responses.Employee;
using ContractManagement.Domains.Interfaces.Employee;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;
using ContractManagement.API.Common.Enums;

namespace ContractManagement.Domains.Controllers.Admin
{
    /// <summary>
    /// API quản lý nhân viên nội bộ.
    /// Dùng cho Admin/Manager tạo tài khoản nhân viên, cập nhật thông tin,
    /// đổi mật khẩu và bật/tắt trạng thái nhân viên.
    /// </summary>
    [Route("api/admin/employees")]
    [ApiController]
    [SessionAuthorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _service;

        public EmployeeController(IEmployeeService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách nhân viên có phân trang và tìm kiếm.
        /// Ví dụ:
        /// GET /api/admin/employees?page=1&pageSize=20&keyword=an
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] EmployeeFilterRequest filter)
        {
            var result = await _service.GetListAsync(filter);

            return Ok(
                ApiResponse<PagedResult<EmployeeResponse>>.Ok(
                    result,
                    "Lấy danh sách nhân viên thành công."));
        }

        /// <summary>
        /// Lấy chi tiết một nhân viên theo EmployeeId.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return Ok(
                ApiResponse<EmployeeResponse>.Ok(
                    result,
                    "Lấy chi tiết nhân viên thành công."));
        }

        /// <summary>
        /// Tạo nhân viên mới.
        /// Password sẽ được hash trong EmployeeService,
        /// không lưu password thô vào database.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateEmployeeRequest request)
        {
            var result = await _service.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.EmployeeId },
                ApiResponse<EmployeeResponse>.Ok(
                    result,
                    "Tạo nhân viên thành công."));
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên.
        /// Không đổi password ở API này.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateEmployeeRequest request)
        {
            await _service.UpdateAsync(id, request);

            return Ok(
                ApiResponse<object>.Ok(
                    new { employeeId = id },
                    "Cập nhật nhân viên thành công."));
        }

        /// <summary>
        /// Đổi/reset mật khẩu nhân viên.
        /// </summary>
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> ChangePassword(
            int id,
            [FromBody] ChangePasswordRequest request)
        {
            await _service.ChangePasswordAsync(id, request);

            return Ok(
                ApiResponse<object>.Ok(
                    new { employeeId = id },
                    "Đổi mật khẩu nhân viên thành công."));
        }

        /// <summary>
        /// Bật/tắt trạng thái nhân viên.
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
                    new { employeeId = id, status },
                    "Cập nhật trạng thái nhân viên thành công."));
        }
    }
}