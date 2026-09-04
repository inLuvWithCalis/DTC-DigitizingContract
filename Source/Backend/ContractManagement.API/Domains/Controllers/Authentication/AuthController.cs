using ContractManagement.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.DTOs.Responses.Authentication;
using ContractManagement.API.Domains.Interfaces.Authentication;
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
        private readonly IEmployeeAccountService? _employeeAccountService;

        private readonly ICurrentTenant _currentTenant;

        public AuthController(
            DbDtctechContext dbDtctechContext,
            IPasswordHasher<TblEmployee> passwordHasher,
            ICurrentTenant currentTenant,
            IEmployeeAccountService? employeeAccountService = null)
        {
            this._dbDtctechContext = dbDtctechContext;
            _passwordHasher = passwordHasher;
            _currentTenant = currentTenant;
            _employeeAccountService = employeeAccountService;
        }

        [HttpGet("profile")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> GetProfile(
            CancellationToken cancellationToken)
        {
            var employeeId = GetAuthenticatedEmployeeId();
            var profile = await AccountService.GetProfileAsync(
                employeeId,
                cancellationToken);
            return Ok(profile);
        }

        [HttpPut("profile")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateEmployeeSelfProfileRequest request,
            CancellationToken cancellationToken)
        {
            var employeeId = GetAuthenticatedEmployeeId();
            var profile = await AccountService.UpdateProfileAsync(
                employeeId,
                request,
                cancellationToken);
            HttpContext.Session.SetString(
                AccountSessionKeys.EmployeeName,
                profile.FullName ?? string.Empty);
            return Ok(profile);
        }

        [HttpGet("profile/avatar")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public Task<IActionResult> GetAvatar(CancellationToken cancellationToken) =>
            OpenProfileImageAsync(ProfileImageKind.Avatar, cancellationToken);

        [HttpPost("profile/avatar")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> UploadAvatar(
            [FromForm] ProfileImageUploadRequest request,
            CancellationToken cancellationToken)
        {
            var profile = await AccountService.UploadProfileImageAsync(
                GetAuthenticatedEmployeeId(),
                ProfileImageKind.Avatar,
                request,
                cancellationToken);
            return Ok(profile);
        }

        [HttpDelete("profile/avatar")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> DeleteAvatar(
            [FromQuery] string rowVersion,
            CancellationToken cancellationToken)
        {
            var profile = await AccountService.DeleteProfileImageAsync(
                GetAuthenticatedEmployeeId(),
                ProfileImageKind.Avatar,
                rowVersion,
                cancellationToken);
            return Ok(profile);
        }

        [HttpGet("profile/cover")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public Task<IActionResult> GetCover(CancellationToken cancellationToken) =>
            OpenProfileImageAsync(ProfileImageKind.Cover, cancellationToken);

        [HttpPost("profile/cover")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> UploadCover(
            [FromForm] ProfileImageUploadRequest request,
            CancellationToken cancellationToken)
        {
            var profile = await AccountService.UploadProfileImageAsync(
                GetAuthenticatedEmployeeId(),
                ProfileImageKind.Cover,
                request,
                cancellationToken);
            return Ok(profile);
        }

        [HttpDelete("profile/cover")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> DeleteCover(
            [FromQuery] string rowVersion,
            CancellationToken cancellationToken)
        {
            var profile = await AccountService.DeleteProfileImageAsync(
                GetAuthenticatedEmployeeId(),
                ProfileImageKind.Cover,
                rowVersion,
                cancellationToken);
            return Ok(profile);
        }

        [HttpPut("password")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
        public async Task<IActionResult> ChangeOwnPassword(
            [FromBody] ChangeOwnPasswordRequest request,
            CancellationToken cancellationToken)
        {
            await AccountService.ChangePasswordAsync(
                GetAuthenticatedEmployeeId(),
                request,
                cancellationToken);
            HttpContext.Session.Clear();
            return Ok(new
            {
                message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
            });
        }

        [HttpGet("preferences")]
        [SessionAuthorize]
        public async Task<IActionResult> GetPreferences(
            CancellationToken cancellationToken)
        {
            var preferences = await AccountService.GetPreferencesAsync(
                GetAuthenticatedEmployeeId(),
                cancellationToken);
            return Ok(preferences);
        }

        [HttpPut("preferences")]
        [SessionAuthorize]
        public async Task<IActionResult> UpdatePreferences(
            [FromBody] UpdateEmployeePreferencesRequest request,
            CancellationToken cancellationToken)
        {
            var preferences = await AccountService.UpdatePreferencesAsync(
                GetAuthenticatedEmployeeId(),
                request,
                cancellationToken);
            return Ok(preferences);
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
                    message = "Sai tên đăng nhập hoặc mật khẩu."
                });
            }

            // 3. Verify password
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(employee, employee.EmployeePassword, request.Password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Sai tên đăng nhập hoặc mật khẩu."
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

            if (passwordVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                employee.EmployeePassword = _passwordHasher.HashPassword(
                    employee,
                    request.Password);
                await _dbDtctechContext.SaveChangesAsync(HttpContext.RequestAborted);
            }

            // 4. Set up session or token (this is a placeholder, implement your own logic)
            HttpContext.Session.SetInt32(
                AccountSessionKeys.EmployeeId,
                employee.EmployeeId);

            HttpContext.Session.SetString(
                AccountSessionKeys.EmployeeName,
                employee.EmployeeFullName ?? "");

            HttpContext.Session.SetInt32(
                AccountSessionKeys.EmployeeSessionVersion,
                employee.SessionVersion);

            HttpContext.Session.SetInt32("TenantId", tenant.TenantId);

            HttpContext.Session.SetString("TenantCode", tenant.TenantCode);

            // 5. Return success response
            return Ok(new
            {
                message = "Đăng nhập thành công.",
                employeeId = employee.EmployeeId,
                employeeName = employee.EmployeeFullName,
                tenantId = tenant.TenantId,
                tenantCode = tenant.TenantCode,
                tenantName = tenant.TenantName
            });
        }

        [HttpGet("me")]
        [SessionAuthorize(AllowWhenPasswordChangeRequired = true)]
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
                employee.Permissions,
                employee.MustChangePassword,
                employee.PasswordChangedAt,
                employee.ImageUrl,
                EmployeePreferenceRoutes.ResolveDefault(
                    employee.DefaultPage,
                    employee.Permissions)));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Return success response
            return Ok(new
            {
                message = "Đăng xuất thành công."
            });
        }

        private IEmployeeAccountService AccountService =>
            _employeeAccountService
            ?? throw new InvalidOperationException(
                "Employee account service is not configured.");

        private int GetAuthenticatedEmployeeId() =>
            EmployeeAuthorizationContext.GetEmployee(HttpContext)?.EmployeeId
            ?? throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "Employee login is required.");

        private async Task<IActionResult> OpenProfileImageAsync(
            ProfileImageKind kind,
            CancellationToken cancellationToken)
        {
            var image = await AccountService.OpenProfileImageAsync(
                GetAuthenticatedEmployeeId(),
                kind,
                cancellationToken);
            Response.Headers.CacheControl = "private, max-age=300";
            return File(image.Content, image.ContentType, enableRangeProcessing: true);
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
