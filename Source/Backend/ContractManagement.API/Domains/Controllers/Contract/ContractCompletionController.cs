using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Contract;

[ApiController]
[Route("api/contracts/{contractId:int}")]
[SessionAuthorize]
public sealed class ContractCompletionController : ControllerBase
{
    private readonly IContractCompletionService _service;
    public ContractCompletionController(IContractCompletionService service) => _service = service;

    [HttpGet("completion")]
    public async Task<IActionResult> Get(int contractId, CancellationToken ct) =>
        Ok(ApiResponse<ContractCompletionDetailResponse>.Ok(await _service.GetAsync(contractId, EmployeeId(), ct)));

    [HttpGet("completion-readiness")]
    public async Task<IActionResult> Readiness(int contractId, CancellationToken ct) =>
        Ok(ApiResponse<ContractCompletionReadinessResponse>.Ok(await _service.GetReadinessAsync(contractId, EmployeeId(), ct)));

    [HttpPost("acceptance-evidence")]
    [SessionAuthorize(RbacPermissions.ContractManageOwn)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAcceptance(int contractId, [FromForm] UploadContractAcceptanceEvidenceRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractAcceptanceEvidenceResponse>.Ok(await _service.UploadAcceptanceAsync(contractId, request, EmployeeId(), ct), "Đã lưu biên bản nghiệm thu."));

    [HttpPost("payments")]
    [SessionAuthorize(RbacPermissions.ContractManageOwn)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddPayment(int contractId, [FromForm] AddContractPaymentRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractPaymentResponse>.Ok(await _service.AddPaymentAsync(contractId, request, EmployeeId(), ct), "Đã ghi nhận khoản thanh toán."));

    [HttpPost("payments/{paymentId:int}/void")]
    [SessionAuthorize(RbacPermissions.ContractManageOwn)]
    public async Task<IActionResult> VoidPayment(int contractId, int paymentId, [FromBody] VoidContractPaymentRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractPaymentResponse>.Ok(await _service.VoidPaymentAsync(contractId, paymentId, request, EmployeeId(), ct), "Đã hủy khoản thanh toán."));

    [HttpPost("complete")]
    [SessionAuthorize(RbacPermissions.ContractComplete)]
    public async Task<IActionResult> Complete(int contractId, [FromBody] CompleteContractRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractCompletionDetailResponse>.Ok(await _service.CompleteAsync(contractId, request, EmployeeId(), ct), "Hợp đồng đã hoàn tất."));

    private int EmployeeId() => HttpContext.Session.GetInt32("EmployeeId")
        ?? throw new RbacOperationException(StatusCodes.Status401Unauthorized, AuthorizationErrorCodes.AuthenticationRequired, "Employee login is required.");
}
