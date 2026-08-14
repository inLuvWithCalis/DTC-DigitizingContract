using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Security;
using ContractManagement.API.Domains.DTOs.Responses.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Security;

/// <summary>
/// Manager-only tenant security audit read model. The records are append-only;
/// this controller deliberately has no create, update or delete route.
/// </summary>
[ApiController]
[Route("api/security-audits")]
[SessionAuthorize(RbacPermissions.SecurityAuditReadTenant)]
public sealed class SecurityAuditsController : ControllerBase
{
    private readonly ITenantSecurityAuditQueryService _queryService;

    public SecurityAuditsController(ITenantSecurityAuditQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<TenantSecurityAuditResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthorizationErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AuthorizationErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        [FromQuery] TenantSecurityAuditFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeId")
            ?? throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee login is required.");

        var result = await _queryService.QueryAsync(
            filter,
            employeeId,
            cancellationToken);
        return Ok(ApiResponse<PagedResult<TenantSecurityAuditResponse>>.Ok(
            result,
            "Lấy security audit của tenant thành công."));
    }
}
