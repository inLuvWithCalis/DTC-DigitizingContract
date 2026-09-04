using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Dashboard;
using ContractManagement.API.Domains.DTOs.Responses.Dashboard;
using ContractManagement.API.Domains.Interfaces.Dashboard;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard")]
[SessionAuthorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthorizationErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromQuery] DashboardFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var employeeId = EmployeeAuthorizationContext.GetEmployee(HttpContext)?.EmployeeId
            ?? throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee login is required.");
        return Ok(await _dashboardService.GetAsync(
            employeeId,
            filter,
            cancellationToken));
    }
}
