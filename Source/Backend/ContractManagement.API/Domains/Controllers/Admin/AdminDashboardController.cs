using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.AdminDashboard;
using ContractManagement.API.Domains.DTOs.Responses.AdminDashboard;
using ContractManagement.API.Domains.Interfaces.Admin;
using ContractManagement.Attributes;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[AllowWithoutTenant]
[SystemAdminAuthorize]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthorizationErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminDashboardResponse>> Get(
        [FromQuery] AdminDashboardFilterRequest filter,
        CancellationToken cancellationToken) =>
        Ok(await _dashboardService.GetAsync(filter, cancellationToken));
}
