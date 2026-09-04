using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Contract;

[ApiController]
[Route("api/contract-approvals")]
[SessionAuthorize]
public sealed class ContractApprovalController : ControllerBase
{
    private readonly IContractApprovalService _service;

    public ContractApprovalController(IContractApprovalService service)
    {
        _service = service;
    }

    [HttpGet]
    [SessionAuthorize(RbacPermissions.ContractApprovalDecide)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ContractApprovalRequestResponse>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInbox(
        [FromQuery] ContractApprovalInboxFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetInboxAsync(
            filter,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<PagedResult<ContractApprovalRequestResponse>>.Ok(
            result,
            "Lấy danh sách hợp đồng chờ duyệt thành công."));
    }

    [HttpGet("{approvalRequestId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<ContractApprovalDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(
        int approvalRequestId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetDetailAsync(
            approvalRequestId,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractApprovalDetailResponse>.Ok(
            result,
            "Lấy chi tiết yêu cầu duyệt thành công."));
    }

    [HttpGet("contracts/{contractId:int}/history")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<ContractApprovalRequestResponse>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractHistory(
        int contractId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetContractHistoryAsync(
            contractId,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ContractApprovalRequestResponse>>.Ok(
            result,
            "Lấy lịch sử duyệt hợp đồng thành công."));
    }

    [HttpPost("{approvalRequestId:int}/approve")]
    [SessionAuthorize(RbacPermissions.ContractApprovalDecide)]
    public Task<IActionResult> Approve(
        int approvalRequestId,
        [FromBody] ContractApprovalDecisionRequest request,
        CancellationToken cancellationToken) =>
        Decide(
            approvalRequestId,
            ApprovalRequestStatus.Approved,
            request,
            "Duyệt hợp đồng thành công.",
            cancellationToken);

    [HttpPost("bulk-decide")]
    [SessionAuthorize(RbacPermissions.ContractApprovalDecide)]
    [ProducesResponseType(
        typeof(ApiResponse<ContractApprovalBulkDecisionResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> DecideBulk(
        [FromBody] ContractApprovalBulkDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.DecideBulkAsync(
            request,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractApprovalBulkDecisionResponse>.Ok(
            result,
            $"Đã xử lý {result.SuccessCount}/{result.TotalCount} yêu cầu duyệt."));
    }

    [HttpPost("{approvalRequestId:int}/return")]
    [SessionAuthorize(RbacPermissions.ContractApprovalDecide)]
    public Task<IActionResult> Return(
        int approvalRequestId,
        [FromBody] ContractApprovalDecisionRequest request,
        CancellationToken cancellationToken) =>
        Decide(
            approvalRequestId,
            ApprovalRequestStatus.Returned,
            request,
            "Trả hợp đồng về chỉnh sửa thành công.",
            cancellationToken);

    [HttpPost("{approvalRequestId:int}/reject")]
    [SessionAuthorize(RbacPermissions.ContractApprovalDecide)]
    public Task<IActionResult> Reject(
        int approvalRequestId,
        [FromBody] ContractApprovalDecisionRequest request,
        CancellationToken cancellationToken) =>
        Decide(
            approvalRequestId,
            ApprovalRequestStatus.Rejected,
            request,
            "Từ chối hợp đồng thành công.",
            cancellationToken);

    [HttpPost("{approvalRequestId:int}/withdraw")]
    [SessionAuthorize(RbacPermissions.ContractManageOwn)]
    public async Task<IActionResult> Withdraw(
        int approvalRequestId,
        [FromBody] WithdrawContractApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.WithdrawAsync(
            approvalRequestId,
            request,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractApprovalActionResponse>.Ok(
            result,
            "Rút yêu cầu duyệt thành công."));
    }

    private async Task<IActionResult> Decide(
        int approvalRequestId,
        ApprovalRequestStatus decision,
        ContractApprovalDecisionRequest request,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var result = await _service.DecideAsync(
            approvalRequestId,
            decision,
            request,
            GetEmployeeId(),
            cancellationToken);
        return Ok(ApiResponse<ContractApprovalActionResponse>.Ok(
            result,
            successMessage));
    }

    private int GetEmployeeId() =>
        HttpContext.Session.GetInt32("EmployeeId")
        ?? throw new RbacOperationException(
            StatusCodes.Status401Unauthorized,
            AuthorizationErrorCodes.AuthenticationRequired,
            "Employee login is required.");
}
