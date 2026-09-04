using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Responses.Admin;
using ContractManagement.API.Domains.Interfaces.Admin;
using ContractManagement.Attributes;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Admin;

[ApiController]
[AllowWithoutTenant]
public sealed class SystemHealthController : ControllerBase
{
    private readonly ISystemHealthService _systemHealthService;

    public SystemHealthController(ISystemHealthService systemHealthService)
    {
        _systemHealthService = systemHealthService;
    }

    [HttpGet("/health/live")]
    public IActionResult Live() => Ok(new { status = "Healthy" });

    [HttpGet("/health/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var ready = await _systemHealthService.IsReadyAsync(cancellationToken);
        var response = new { status = ready ? "Ready" : "Unavailable" };
        return ready
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    [HttpGet("/api/admin/system-health")]
    [SystemAdminAuthorize]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthorizationErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SystemHealthResponse>> Details(
        CancellationToken cancellationToken) =>
        Ok(await _systemHealthService.GetDetailedAsync(cancellationToken));
}
