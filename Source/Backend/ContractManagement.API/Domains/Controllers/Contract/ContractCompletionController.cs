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

    [HttpGet("versions/{versionId:int}/payment-milestones")]
    public async Task<IActionResult> GetPaymentMilestones(int contractId, int versionId,
        CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<ContractPaymentMilestoneResponse>>.Ok(
            await _service.GetPaymentMilestonesAsync(contractId, versionId, EmployeeId(), ct)));

    [HttpPost("acceptance-evidence")]
    [SessionAuthorize(RbacPermissions.ContractAcceptanceManage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAcceptance(int contractId, [FromForm] UploadContractAcceptanceEvidenceRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractAcceptanceEvidenceResponse>.Ok(await _service.UploadAcceptanceAsync(contractId, request, EmployeeId(), ct), "Đã lưu biên bản nghiệm thu."));

    [HttpPost("payments")]
    [SessionAuthorize(RbacPermissions.ContractPaymentManage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddPayment(int contractId, [FromForm] AddContractPaymentRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractPaymentResponse>.Ok(await _service.AddPaymentAsync(contractId, request, EmployeeId(), ct), "Đã ghi nhận khoản thanh toán."));

    [HttpPost("versions/{versionId:int}/payment-milestones/{milestoneId:int}/complete")]
    [SessionAuthorize(RbacPermissions.ContractPaymentManage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CompletePaymentMilestone(
        int contractId, int versionId, int milestoneId,
        [FromForm] CompleteContractPaymentMilestoneRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<CompletePaymentMilestoneResponse>.Ok(
            await _service.CompletePaymentMilestoneAsync(contractId, versionId,
                milestoneId, request, EmployeeId(), ct),
            "Đã hoàn tất đợt thanh toán và lưu chứng từ."));

    [HttpPost("versions/{versionId:int}/payment-milestones/{milestoneId:int}/reopen")]
    [SessionAuthorize(RbacPermissions.ContractPaymentManage)]
    public async Task<IActionResult> ReopenPaymentMilestone(
        int contractId, int versionId, int milestoneId,
        [FromBody] ReopenContractPaymentMilestoneRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<ReopenPaymentMilestoneResponse>.Ok(
            await _service.ReopenPaymentMilestoneAsync(contractId, versionId,
                milestoneId, request, EmployeeId(), ct),
            "Đã chuyển đợt thanh toán về chưa nộp và lưu lịch sử chứng từ."));

    [HttpPost("payments/{paymentId:int}/void")]
    [SessionAuthorize(RbacPermissions.ContractPaymentManage)]
    public async Task<IActionResult> VoidPayment(int contractId, int paymentId, [FromBody] VoidContractPaymentRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractPaymentResponse>.Ok(await _service.VoidPaymentAsync(contractId, paymentId, request, EmployeeId(), ct), "Đã hủy khoản thanh toán."));

    [HttpPost("complete")]
    [SessionAuthorize(RbacPermissions.ContractComplete)]
    public async Task<IActionResult> Complete(int contractId, [FromBody] CompleteContractRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ContractCompletionDetailResponse>.Ok(await _service.CompleteAsync(contractId, request, EmployeeId(), ct), "Hợp đồng đã hoàn tất."));

    private int EmployeeId() => HttpContext.Session.GetInt32("EmployeeId")
        ?? throw new RbacOperationException(StatusCodes.Status401Unauthorized, AuthorizationErrorCodes.AuthenticationRequired, "Employee login is required.");
}
