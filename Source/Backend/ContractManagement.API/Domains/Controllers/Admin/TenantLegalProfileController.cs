using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.LegalProfiles;
using ContractManagement.API.Domains.DTOs.Responses.LegalProfiles;
using ContractManagement.API.Domains.Interfaces.LegalProfiles;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Admin;

[Route("api/admin/tenant-legal-profile")]
[ApiController]
[SessionAuthorize(RbacPermissions.TenantLegalProfileManage)]
public sealed class TenantLegalProfileController : ControllerBase
{
    private readonly ITenantLegalProfileService _service;

    public TenantLegalProfileController(ITenantLegalProfileService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(cancellationToken);
        return Ok(ApiResponse<TenantLegalProfileResponse?>.Ok(
            result,
            result is null
                ? "Tenant chưa cấu hình hồ sơ pháp lý."
                : "Lấy hồ sơ pháp lý thành công."));
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertTenantLegalProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpsertAsync(
            request,
            GetEmployeeId(),
            cancellationToken);

        return Ok(ApiResponse<TenantLegalProfileResponse>.Ok(
            result,
            "Lưu hồ sơ pháp lý thành công."));
    }

    private int GetEmployeeId()
    {
        return HttpContext.Session.GetInt32("EmployeeId")
            ?? throw new UnauthorizedAccessException(
                "Không xác định được nhân viên đăng nhập.");
    }
}
