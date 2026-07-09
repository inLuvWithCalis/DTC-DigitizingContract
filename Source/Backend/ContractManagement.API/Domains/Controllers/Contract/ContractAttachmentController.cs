using ContractManagement.API.Common.Responses;
using ContractManagement.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Contract
{
    /// <summary>
    /// API quản lý file đính kèm riêng cho hợp đồng.
    /// </summary>
    [Route("api/contracts/{contractId:int}/attachments")]
    [ApiController]
    [SessionAuthorize]
    public class ContractAttachmentController : ControllerBase
    {
        private readonly IContractAttachmentService _service;

        public ContractAttachmentController(IContractAttachmentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            int contractId,
            [FromForm] UploadContractAttachmentRequest request)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _service.UploadAsync(
                contractId,
                request,
                employeeId.Value);

            return Ok(
                ApiResponse<ContractAttachmentResponse>.Ok(
                    result,
                    "Upload file đính kèm hợp đồng thành công."));
        }

        [HttpGet]
        public async Task<IActionResult> GetByContract(int contractId)
        {
            var result = await _service.GetByContractAsync(contractId);

            return Ok(
                ApiResponse<List<ContractAttachmentResponse>>.Ok(
                    result,
                    "Lấy danh sách file đính kèm hợp đồng thành công."));
        }

        [HttpDelete("{attachmentId:int}")]
        public async Task<IActionResult> Delete(
            int contractId,
            int attachmentId)
        {
            await _service.DeleteAsync(contractId, attachmentId);

            return Ok(
                ApiResponse<object>.Ok(
                    new { contractId, attachmentId },
                    "Xóa file đính kèm hợp đồng thành công."));
        }
    }
}