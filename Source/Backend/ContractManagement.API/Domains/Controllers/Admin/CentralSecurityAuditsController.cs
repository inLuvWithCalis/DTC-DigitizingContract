using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Security;
using ContractManagement.API.Domains.DTOs.Responses.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Attributes;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Controllers.Admin;

/// <summary>
/// System Admin-only Central security audit read model. Tenant business data
/// and tenant Contract audits are intentionally not exposed here.
/// </summary>
[ApiController]
[Route("api/admin/security-audits")]
[AllowWithoutTenant]
[SystemAdminAuthorize]
public sealed class CentralSecurityAuditsController : ControllerBase
{
    private readonly ICentralSecurityAuditQueryService _queryService;

    public CentralSecurityAuditsController(
        ICentralSecurityAuditQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<CentralSecurityAuditResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthorizationErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] CentralSecurityAuditFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var systemAdminId = HttpContext.Session.GetInt32("SystemAdminId")
            ?? throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "System Admin login is required.");

        var result = await _queryService.QueryAsync(
            filter,
            systemAdminId,
            cancellationToken);
        return Ok(ApiResponse<PagedResult<CentralSecurityAuditResponse>>.Ok(
            result,
            "Lấy Central security audit thành công."));
    }
}
