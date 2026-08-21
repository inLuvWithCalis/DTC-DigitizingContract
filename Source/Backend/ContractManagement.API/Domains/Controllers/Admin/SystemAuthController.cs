using ContractManagement.Attributes;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Domains.DTOs.Requests.SystemAuth;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Controllers.SystemAuth;

[ApiController]
[Route("api/system-auth")]
[AllowWithoutTenant]
public sealed class SystemAuthController : ControllerBase
{
    private readonly CentralDbContext _centralDbContext;
    private readonly IPasswordHasher<SystemAdmin> _passwordHasher;
    private readonly ICentralSecurityAuditWriter _securityAuditWriter;

    public SystemAuthController(
        CentralDbContext centralDbContext,
        IPasswordHasher<SystemAdmin> passwordHasher,
        ICentralSecurityAuditWriter securityAuditWriter)
    {
        _centralDbContext = centralDbContext;
        _passwordHasher = passwordHasher;
        _securityAuditWriter = securityAuditWriter;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] SystemAdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var admin = await _centralDbContext.SystemAdmins
            .FirstOrDefaultAsync(
                x => x.Username == request.Username
                     && x.IsActive,
                cancellationToken);

        if (admin is null)
        {
            await WriteLoginAuditAsync(
                null,
                AuthorizationAuditResultTypes.Denied,
                AuthorizationErrorCodes.AuthenticationRequired,
                cancellationToken);
            return Unauthorized(new AuthorizationErrorResponse(
                AuthorizationErrorCodes.AuthenticationRequired,
                "Sai tên đăng nhập hoặc mật khẩu."));
        }

        var result = _passwordHasher.VerifyHashedPassword(
            admin,
            admin.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            await WriteLoginAuditAsync(
                admin.SystemAdminId,
                AuthorizationAuditResultTypes.Denied,
                AuthorizationErrorCodes.AuthenticationRequired,
                cancellationToken);
            return Unauthorized(new AuthorizationErrorResponse(
                AuthorizationErrorCodes.AuthenticationRequired,
                "Sai tên đăng nhập hoặc mật khẩu."));
        }

        HttpContext.Session.SetInt32(
            "SystemAdminId",
            admin.SystemAdminId);

        HttpContext.Session.SetString(
            "SystemAdminName",
            admin.FullName);

        await WriteLoginAuditAsync(
            admin.SystemAdminId,
            AuthorizationAuditResultTypes.Success,
            null,
            cancellationToken);

        return Ok(new
        {
            message = "System admin đăng nhập thành công.",
            systemAdminId = admin.SystemAdminId,
            fullName = admin.FullName
        });
    }

    [HttpGet("me")]
    [SystemAdminAuthorize]
    public async Task<IActionResult> Me(
        CancellationToken cancellationToken)
    {
        int? systemAdminId =
            HttpContext.Session.GetInt32("SystemAdminId");

        if (systemAdminId is null)
        {
            return Unauthorized(new
            {
                message = "System admin is not logged in."
            });
        }

        var admin = await _centralDbContext.SystemAdmins
            .Where(x => x.SystemAdminId == systemAdminId.Value)
            .Select(x => new
            {
                x.SystemAdminId,
                x.Username,
                x.FullName,
                x.Email,
                x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return admin is null
            ? Unauthorized(new { message = "System admin not found." })
            : Ok(admin);
    }

    [HttpPost("logout")]
    [SystemAdminAuthorize]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("SystemAdminId");
        HttpContext.Session.Remove("SystemAdminName");

        return Ok(new
        {
            message = "System admin đăng xuất thành công."
        });
    }

    private Task WriteLoginAuditAsync(
        int? systemAdminId,
        string result,
        string? failureCode,
        CancellationToken cancellationToken) =>
        _securityAuditWriter.TryWriteAsync(
            HttpContext,
            new CentralSecurityAuditWriteRequest(
                systemAdminId,
                null,
                null,
                AuthorizationAuditActionTypes.SystemAdminLogin,
                result,
                "SystemAdmin",
                systemAdminId?.ToString(),
                failureCode),
            cancellationToken);
}
