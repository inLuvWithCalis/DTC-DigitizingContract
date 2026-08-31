using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Contract;

[ApiController]
[Route("api/contracts/{contractId:int}/signing")]
[SessionAuthorize]
public sealed class ContractSigningController : ControllerBase
{
    private readonly IContractSigningService _service;

    public ContractSigningController(IContractSigningService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<ContractSigningDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        int contractId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(
            contractId,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractSigningDetailResponse>.Ok(
            result,
            "Lấy hồ sơ ký hợp đồng thành công."));
    }

    [HttpPost("evidence")]
    [SessionAuthorize(RbacPermissions.ContractManageOwn)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        int contractId,
        [FromForm] UploadContractSignedEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UploadAsync(
            contractId,
            request,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractSignedEvidenceResponse>.Ok(
            result,
            "Đã lưu bản scan và chuyển hợp đồng sang Đã ký."));
    }

    [HttpPost("evidence/{signedEvidenceId:int}/supersede")]
    [SessionAuthorize(RbacPermissions.ContractManageOwn)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Supersede(
        int contractId,
        int signedEvidenceId,
        [FromForm] SupersedeContractSignedEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SupersedeAsync(
            contractId,
            signedEvidenceId,
            request,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractSignedEvidenceResponse>.Ok(
            result,
            "Đã thay bản scan hợp đồng ký."));
    }

    private int GetEmployeeId() =>
        HttpContext.Session.GetInt32("EmployeeId")
        ?? throw new RbacOperationException(
            StatusCodes.Status401Unauthorized,
            AuthorizationErrorCodes.AuthenticationRequired,
            "Employee login is required.");
}
