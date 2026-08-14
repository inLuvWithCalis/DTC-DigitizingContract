using ContractManagement.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Responses.Authentication;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoginRequest = ContractManagement.Domains.DTOs.Requests.Authentication.LoginRequest;

namespace ContractManagement.Domains.Controllers.Authentication
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DbDtctechContext _dbDtctechContext;
        private readonly IPasswordHasher<TblEmployee> _passwordHasher;

        private readonly ICurrentTenant _currentTenant;

        public AuthController(DbDtctechContext dbDtctechContext, IPasswordHasher<TblEmployee> passwordHasher, ICurrentTenant currentTenant)
        {
            this._dbDtctechContext = dbDtctechContext;
            _passwordHasher = passwordHasher;
            _currentTenant = currentTenant;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromHeader(Name = "X-Tenant-Code")] string tenantCode,[FromBody] LoginRequest request)
        {
            // 0. Get current tenant (if needed for multi-tenancy)
            var tenant = _currentTenant.GetRequiredTenant();

            // 1. Get employee by account name
            var employee = await _dbDtctechContext.TblEmployees.FirstOrDefaultAsync(e => e.EmployeeAccount == request.AccountName);

            // 2. Check if employee exists
            if (employee == null
                || string.IsNullOrEmpty(employee.EmployeePassword)
                || string.IsNullOrEmpty(request.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid account name or password."
                });
            }

            // 3. Verify password
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(employee, employee.EmployeePassword, request.Password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid account name or password."
                });
            }

            if (employee.Status != 1)
            {
                return Unauthorized(new AuthorizationErrorResponse(
                    AuthorizationErrorCodes.EmployeeInactive,
                    "Employee account is inactive."));
            }

            if (!EmployeePermissionCatalog.TryGetPermissions(
                    employee.EmployeeType,
                    out _))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new AuthorizationErrorResponse(
                        AuthorizationErrorCodes.PermissionDenied,
                        "Employee role is not valid for RBAC v1."));
            }

            // 4. Set up session or token (this is a placeholder, implement your own logic)
            HttpContext.Session.SetInt32("EmployeeId", employee.EmployeeId);

            HttpContext.Session.SetString("EmployeeName", employee.EmployeeFullName ?? "");

            HttpContext.Session.SetInt32("TenantId", tenant.TenantId);

            HttpContext.Session.SetString("TenantCode", tenant.TenantCode);

            // 5. Return success response
            return Ok(new
            {
                message = "Login successful.",
                employeeId = employee.EmployeeId,
                employeeName = employee.EmployeeFullName,
                tenantId = tenant.TenantId,
                tenantCode = tenant.TenantCode,
                tenantName = tenant.TenantName
            });
        }

        [HttpGet("me")]
        [SessionAuthorize]
        public IActionResult GetCurrentUsers()
        {
            var employee = EmployeeAuthorizationContext.GetEmployee(HttpContext);

            if (employee is null)
            {
                return Unauthorized(new AuthorizationErrorResponse(
                    AuthorizationErrorCodes.AuthenticationRequired,
                    "Employee login is required."));
            }

            var tenant = _currentTenant.GetRequiredTenant();

            return Ok(new AuthMeResponse(
                employee.EmployeeId,
                employee.Account,
                employee.FullName,
                (byte)employee.EmployeeType,
                employee.EmployeeType.ToString(),
                tenant.TenantId,
                tenant.TenantCode,
                tenant.TenantName,
                RbacPermissions.Version,
                employee.Permissions));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Return success response
            return Ok(new
            {
                message = "Logout successful."
            });
        }

        // [HttpGet("generate-hash")]
        // public IActionResult GenerateHash(string password = "123456")
        // {
        //     // Khởi tạo Hasher
        //     var hasher = new PasswordHasher<TblEmployee>();
            
        //     // Tham số TblEmployee có thể để null nếu bạn chỉ muốn lấy chuỗi hash
        //     var hashedPassword = hasher.HashPassword(null!, password);

        //     return Ok(new 
        //     { 
        //         PlainText = password, 
        //         HashedPassword = hashedPassword,
        //         Instruction = "Copy chuỗi HashedPassword này và dán vào cột EmployeePassword trong SQL"
        //     });
        // }
    }
}
