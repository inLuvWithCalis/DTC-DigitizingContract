using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Contract;

/// <summary>
/// Tra cứu lịch sử audit an toàn của Contract trong tenant hiện tại.
/// </summary>
[ApiController]
[Route("api/contract-audits")]
[SessionAuthorize]
public sealed class ContractAuditController : ControllerBase
{
    private readonly IContractAuditQueryService _queryService;

    public ContractAuditController(IContractAuditQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>
    /// Lấy lịch sử audit theo Contract hoặc trên toàn tenant,
    /// tùy theo quyền của nhân viên.
    /// </summary>
    /// <remarks>
    /// Manager và Admin Officer có thể lọc lịch sử audit trên toàn tenant.
    ///
    /// Nhân viên phụ trách phải truyền ContractId và chỉ được xem lịch sử
    /// của Contract mà mình hiện đang phụ trách.
    ///
    /// Response không chứa nội dung comment, số điện thoại, OTP, token,
    /// cookie, hash, payload của worker hoặc dữ liệu nội bộ nhạy cảm.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ContractAuditResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        [FromQuery] ContractAuditFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeId")
            ?? throw new UnauthorizedAccessException(
                "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn.");

        try
        {
            var result = await _queryService.QueryAsync(
                filter,
                employeeId,
                cancellationToken);

            return Ok(
                ApiResponse<PagedResult<ContractAuditResponse>>.Ok(
                    result,
                    "Lấy nhật ký hợp đồng thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail(exception.Message));
        }
    }
}