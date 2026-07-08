using ContractManagement.API.Domains.DTOs.Requests.Department;
using ContractManagement.API.Domains.DTOs.Responses.Department;
using ContractManagement.API.Domains.Interfaces.Department;
using ContractManagement.API.Common.Responses;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Admin
{
    [Route("api/admin/departments")]
    [ApiController]
    [SessionAuthorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _service;

        public DepartmentController(IDepartmentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(ApiResponse<List<DepartmentResponse>>.Ok(
                result,
                "Lấy danh sách phòng ban thành công."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync((short)id);

            return Ok(ApiResponse<DepartmentResponse>.Ok(
                result,
                "Lấy chi tiết phòng ban thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateDepartmentRequest request)
        {
            var result = await _service.CreateAsync(request);

            return Ok(ApiResponse<DepartmentResponse>.Ok(
                result,
                "Tạo phòng ban thành công."));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateDepartmentRequest request)
        {
            await _service.UpdateAsync((short)id, request);

            return Ok(ApiResponse<object>.Ok(
                new { departmentId = id },
                "Cập nhật phòng ban thành công."));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromQuery] byte status)
        {
            await _service.SetStatusAsync((short)id, status);

            return Ok(ApiResponse<object>.Ok(
                new { departmentId = id, status },
                "Cập nhật trạng thái phòng ban thành công."));
        }
    }
}