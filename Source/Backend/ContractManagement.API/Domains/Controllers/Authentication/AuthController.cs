using ContractManagement.Domains.DTOs.Requests.Authentication;
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
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // 0. Get current tenant (if needed for multi-tenancy)
            var tenant = _currentTenant.GetRequiredTenant();

            // 1. Get employee by account name
            var employee = await _dbDtctechContext.TblEmployees.FirstOrDefaultAsync(e => e.EmployeeAccount == request.AccountName);

            // 2. Check if employee exists
            if (employee == null || string.IsNullOrEmpty(request.Password))
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
        public async Task<IActionResult> GetCurrentUsers()
        {
            // 1. Get employee ID from session
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            // 2. Check if employee ID exists in session
            if (employeeId == null)
            {
                return Unauthorized(new
                {
                    message = "User is not logged in."
                });
            }

            // 3. Get employee details from database
            var employee = await _dbDtctechContext.TblEmployees
                                                  .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value);

            // 4. Return results
            return employee == null ? Unauthorized("User not found!") : Ok(employee);
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
    }
}
