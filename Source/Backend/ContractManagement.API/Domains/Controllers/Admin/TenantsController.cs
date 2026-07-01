using ContractManagement.Attributes;
using ContractManagement.Contracts.Tenants;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.MultiTenancy.Contracts;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[AllowWithoutTenant]
[SystemAdminAuthorize]

/*
 * Khi hoàn thành phân quyền, thêm:
 *
 * [SessionAuthorize("SystemAdmin")]
 *
 * hoặc:
 *
 * [Authorize(Roles = "SystemAdmin")]
 */
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService
        _tenantProvisioningService;

    public TenantsController(
        ITenantProvisioningService tenantProvisioningService)
    {
        _tenantProvisioningService =
            tenantProvisioningService;
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
                request.TenantName);

        var result =
            await _tenantProvisioningService
                .CreateDedicatedAsync(
                    command,
                    cancellationToken);

        var response = new TenantResponse
        {
            TenantId = result.TenantId,
            TenantCode = result.TenantCode,
            TenantName = result.TenantName,
            DatabaseName = result.DatabaseName,
            DatabaseMode = result.DatabaseMode,
            Status = result.Status
        };

        return Created(
            $"/api/admin/tenants/{response.TenantId}",
            response);
    }
}