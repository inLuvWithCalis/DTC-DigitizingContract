using ContractManagement.Attributes;
using ContractManagement.Domains.DTOs.Requests.SystemAuth;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
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

    public SystemAuthController(
        CentralDbContext centralDbContext,
        IPasswordHasher<SystemAdmin> passwordHasher)
    {
        _centralDbContext = centralDbContext;
        _passwordHasher = passwordHasher;
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
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        var result = _passwordHasher.VerifyHashedPassword(
            admin,
            admin.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        HttpContext.Session.SetInt32(
            "SystemAdminId",
            admin.SystemAdminId);

        HttpContext.Session.SetString(
            "SystemAdminName",
            admin.FullName);

        return Ok(new
        {
            message = "System admin login successful.",
            systemAdminId = admin.SystemAdminId,
            fullName = admin.FullName
        });
    }

    [HttpGet("me")]
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
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("SystemAdminId");
        HttpContext.Session.Remove("SystemAdminName");

        return Ok(new
        {
            message = "System admin logout successful."
        });
    }
}