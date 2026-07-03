using ContractManagement.Domains.DTOs.Requests;
using ContractManagement.Domains.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            // 1. Check input validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. Create quotation
            try
            {
                int currentEmployeeId = 1; // TODO: Get from auth context
                var result = await _service.CreateQuotationAsync(request, currentEmployeeId);

                return CreatedAtAction(nameof(CreateQuotation), new { id = result.QuotationId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuotations()
        {
            try
            {
                var result = await _service.GetAllQuotationsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuotationById(int id)
        {
            try
            {
                var result = await _service.GetQuotationByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuotation(int id)
        {
            try
            {
                await _service.DeleteQuotationAsync(id);
                return Ok(new { message = "Xóa báo giá thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
