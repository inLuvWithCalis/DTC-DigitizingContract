using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.ContractTemplate;

public sealed partial class ContractTemplateController
{
    [HttpPost("versions/{versionId:int}/appendices")]
    public async Task<IActionResult> AddAppendix(int versionId,
        [FromBody] CreateContractTemplateAppendixRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        var result = await _service.AddAppendixAsync(versionId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<ContractTemplateAppendixResponse>.Ok(result, "Thêm phụ lục mẫu thành công."));
    }

    [HttpPut("versions/{versionId:int}/appendices/order")]
    public async Task<IActionResult> ReorderAppendices(int versionId,
        [FromBody] ReorderContractTemplateAppendicesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        var result = await _service.ReorderAppendicesAsync(versionId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(result, "Sắp xếp phụ lục mẫu thành công."));
    }

    [HttpPut("versions/{versionId:int}/appendices/{appendixId:int}")]
    public async Task<IActionResult> UpdateAppendix(int versionId, int appendixId,
        [FromBody] UpdateContractTemplateAppendixRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        var result = await _service.UpdateAppendixAsync(versionId, appendixId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<ContractTemplateAppendixResponse>.Ok(result, "Cập nhật phụ lục mẫu thành công."));
    }

    [HttpDelete("versions/{versionId:int}/appendices/{appendixId:int}")]
    public async Task<IActionResult> DeleteAppendix(int versionId, int appendixId,
        [FromBody] DeleteContractTemplateAppendixRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        await _service.DeleteAppendixAsync(versionId, appendixId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { versionId, appendixId }, "Xóa phụ lục mẫu thành công."));
    }

    [HttpPost("versions/{versionId:int}/appendices/{appendixId:int}/terms")]
    public async Task<IActionResult> AddAppendixTerm(int versionId, int appendixId,
        [FromBody] CreateContractTemplateAppendixTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        var result = await _service.AddAppendixTermAsync(versionId, appendixId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<ContractTemplateAppendixTermResponse>.Ok(result, "Thêm điều khoản phụ lục thành công."));
    }

    [HttpPut("versions/{versionId:int}/appendices/{appendixId:int}/terms/order")]
    public async Task<IActionResult> ReorderAppendixTerms(int versionId, int appendixId,
        [FromBody] ReorderContractTemplateAppendixTermsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        var result = await _service.ReorderAppendixTermsAsync(versionId, appendixId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<ContractTemplateAppendixResponse>.Ok(result, "Sắp xếp điều khoản phụ lục thành công."));
    }

    [HttpPut("versions/{versionId:int}/appendices/{appendixId:int}/terms/{termId:int}")]
    public async Task<IActionResult> UpdateAppendixTerm(int versionId, int appendixId,
        int termId, [FromBody] UpdateContractTemplateAppendixTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        var result = await _service.UpdateAppendixTermAsync(versionId, appendixId, termId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<ContractTemplateAppendixTermResponse>.Ok(result, "Cập nhật điều khoản phụ lục thành công."));
    }

    [HttpDelete("versions/{versionId:int}/appendices/{appendixId:int}/terms/{termId:int}")]
    public async Task<IActionResult> DeleteAppendixTerm(int versionId, int appendixId,
        int termId, [FromBody] DeleteContractTemplateAppendixTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized)) return unauthorized!;
        await _service.DeleteAppendixTermAsync(versionId, appendixId, termId, request, employeeId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { versionId, appendixId, termId }, "Xóa điều khoản phụ lục thành công."));
    }
}
