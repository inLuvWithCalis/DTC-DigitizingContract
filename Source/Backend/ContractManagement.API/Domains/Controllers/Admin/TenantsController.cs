using ContractManagement.Attributes;
using ContractManagement.Contracts.Tenants;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.MultiTenancy.Contracts;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.Domains.Interfaces.Employee;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[AllowWithoutTenant]
[SystemAdminAuthorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService
        _tenantProvisioningService;
    private readonly ISystemAdminManagerGovernanceService
        _managerGovernanceService;

    public TenantsController(
        ITenantProvisioningService tenantProvisioningService,
        ISystemAdminManagerGovernanceService managerGovernanceService)
    {
        _tenantProvisioningService =
            tenantProvisioningService;
        _managerGovernanceService = managerGovernanceService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var tenants = await _tenantProvisioningService.GetAllAsync(cancellationToken);
        return Ok(tenants.Select(MapTenantResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponse>>
        CreateDedicatedTenant(
            [FromBody] CreateTenantRequest request,
            CancellationToken cancellationToken)
    {
        var command =
            new TenantProvisioningCommand(
                request.TenantCode,
                request.TenantName,
                new InitialManagerProvisioningCommand(
                    request.InitialManager.EmployeeCode,
                    request.InitialManager.EmployeeAccount,
                    request.InitialManager.EmployeePassword,
                    request.InitialManager.EmployeeFullName,
                    request.InitialManager.EmployeeMobile,
                    request.InitialManager.EmployeeEmail),
                new SecurityOperationContext(
                    GetSystemAdminId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString(),
                    HttpContext.TraceIdentifier));

        var result =
            await _tenantProvisioningService
                .CreateDedicatedAsync(
                    command,
                    cancellationToken);

        var response = MapTenantResponse(result);

        return Created(
            $"/api/admin/tenants/{response.TenantId}",
            response);
    }

    [HttpPut("{tenantCode}/employees/{employeeId:int}/role")]
    public async Task<IActionResult> ChangeManagerRole(
        string tenantCode,
        int employeeId,
        [FromBody] ChangeEmployeeRoleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _managerGovernanceService
            .ChangeManagerRoleAsync(
                GetSystemAdminId(),
                tenantCode,
                employeeId,
                request,
                cancellationToken);

        return Ok(response);
    }

    private int GetSystemAdminId()
    {
        return HttpContext.Session.GetInt32("SystemAdminId")
            ?? throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "System Admin login is required.");
    }

    private static TenantResponse MapTenantResponse(TenantProvisioningResult result) =>
        new()
        {
            TenantId = result.TenantId,
            TenantCode = result.TenantCode,
            TenantName = result.TenantName,
            DatabaseName = result.DatabaseName,
            DatabaseMode = result.DatabaseMode,
            Status = result.Status
        };
}
