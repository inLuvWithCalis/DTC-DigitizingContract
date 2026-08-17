using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.DTOs.Requests.Quotation;
using ContractManagement.Domains.DTOs.Responses.Quotation;
using ContractManagement.Domains.Interfaces.Quotation;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Quotation
{
    [Route("api/[controller]")]
    [ApiController]
    [SessionAuthorize(RbacPermissions.QuotationManage)]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _service;

        public QuotationController(IQuotationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuotation([FromBody] CreateQuotationRequestDto request)
        {
            // 1. Validate dữ liệu request.
            // Nếu DTO thiếu field hoặc sai rule thì trả lỗi 400.
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Dữ liệu báo giá không hợp lệ.",
                        errors));
            }

            // 2. Lấy EmployeeId từ Session.
            var currentEmployeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            // 3. Gọi service tạo báo giá.
            // Nếu service throw exception, middleware sẽ tự bắt.
            var result = await _service.CreateQuotationAsync(
                request,
                currentEmployeeId.Value);

            // 4. Trả response chuẩn cho frontend.
            return CreatedAtAction(
                nameof(GetQuotationById),
                new { id = result.QuotationId },
                ApiResponse<QuotationResponseDto>.Ok(
                    result,
                    "Create quotation successfully!"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuotations()
        {
            var result = await _service.GetAllQuotationsAsync();

            return Ok(ApiResponse<List<QuotationResponseDto>>.Ok(result, "Get all quotations successfully!"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuotationById(int id)
        {
            var result = await _service.GetQuotationByIdAsync(id);

            return Ok(ApiResponse<QuotationResponseDto>.Ok(result, "Get quotation by ID successfully!"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuotation(int id)
        {
            var result = await _service.DeleteQuotationAsync(id);

            return Ok(
                ApiResponse<object>.Ok(result, "Delete quotation successfully!"));
        }
    }
}
