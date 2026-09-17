using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Contract;

[Route("api/contracts/{contractId:int}/versions/{versionId:int}/appendices")]
[ApiController]
[SessionAuthorize]
public sealed class ContractAppendixController : ControllerBase
{
    private readonly IContractService _service;

    public ContractAppendixController(IContractService service) =>
        _service = service;

    [HttpPost]
    public async Task<IActionResult> Add(int contractId, int versionId,
        [FromBody] AddContractAppendixRequest request)
    {
        var result = await _service.AddAppendixAsync(contractId, versionId,
            request, GetEmployeeId());
        return Ok(ApiResponse<ContractAppendixResponse>.Ok(result,
            "Thêm phụ lục hợp đồng thành công."));
    }

    [HttpDelete("{appendixId:int}")]
    public async Task<IActionResult> Delete(int contractId, int versionId,
        int appendixId, [FromBody] DeleteContractAppendixRequest request)
    {
        await _service.DeleteAppendixAsync(contractId, versionId, appendixId,
            request, GetEmployeeId());
        return Ok(ApiResponse<object>.Ok(new { appendixId },
            "Xóa phụ lục hợp đồng thành công."));
    }

    [HttpPost("{appendixId:int}/terms")]
    public async Task<IActionResult> AddTerm(int contractId, int versionId,
        int appendixId, [FromBody] CreateContractAppendixTermRequest request)
    {
        var result = await _service.AddAppendixTermAsync(contractId, versionId,
            appendixId, request, GetEmployeeId());
        return Ok(ApiResponse<ContractAppendixTermResponse>.Ok(result,
            "Thêm điều khoản phụ lục thành công."));
    }

    [HttpPut("{appendixId:int}/terms/{termId:int}")]
    public async Task<IActionResult> UpdateTerm(int contractId, int versionId,
        int appendixId, int termId,
        [FromBody] UpdateContractAppendixTermRequest request)
    {
        var result = await _service.UpdateAppendixTermAsync(contractId,
            versionId, appendixId, termId, request, GetEmployeeId());
        return Ok(ApiResponse<ContractAppendixTermResponse>.Ok(result,
            "Cập nhật điều khoản phụ lục thành công."));
    }

    [HttpDelete("{appendixId:int}/terms/{termId:int}")]
    public async Task<IActionResult> DeleteTerm(int contractId, int versionId,
        int appendixId, int termId,
        [FromBody] DeleteContractAppendixTermRequest request)
    {
        await _service.DeleteAppendixTermAsync(contractId, versionId,
            appendixId, termId, request, GetEmployeeId());
        return Ok(ApiResponse<object>.Ok(new { appendixId, termId },
            "Xóa điều khoản phụ lục thành công."));
    }

    [HttpPut("{appendixId:int}/terms/order")]
    public async Task<IActionResult> ReorderTerms(int contractId, int versionId,
        int appendixId,
        [FromBody] ReorderContractAppendixTermsRequest request)
    {
        var result = await _service.ReorderAppendixTermsAsync(contractId,
            versionId, appendixId, request, GetEmployeeId());
        return Ok(ApiResponse<ContractAppendixResponse>.Ok(result,
            "Sắp xếp điều khoản phụ lục thành công."));
    }

    private int GetEmployeeId() =>
        HttpContext.Session.GetInt32("EmployeeId")
        ?? throw new UnauthorizedAccessException(
            "Bạn chưa đăng nhập hoặc session đã hết hạn.");
}
