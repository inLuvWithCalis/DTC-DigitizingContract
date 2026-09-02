using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.DTOs.Responses.Authentication;
using ContractManagement.API.Domains.Interfaces.Authentication;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.File;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Authentication;

public sealed class EmployeeAccountService : IEmployeeAccountService
{
    private readonly DbDtctechContext _dbContext;
    private readonly IPasswordHasher<TblEmployee> _passwordHasher;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPrivateFileStorage _privateFileStorage;
    private readonly ILogger<EmployeeAccountService> _logger;

    public EmployeeAccountService(
        DbDtctechContext dbContext,
        IPasswordHasher<TblEmployee> passwordHasher,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor,
        IPrivateFileStorage privateFileStorage,
        ILogger<EmployeeAccountService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
        _privateFileStorage = privateFileStorage;
        _logger = logger;
    }

    public async Task<EmployeeProfileResponse> GetProfileAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeAsync(
            employeeId,
            asTracking: false,
            cancellationToken);
        var departmentName = await GetDepartmentNameAsync(
            employee.DepartmentId,
            cancellationToken);
        return Map(employee, departmentName);
    }

    public async Task<EmployeeProfileResponse> UpdateProfileAsync(
        int employeeId,
        UpdateEmployeeSelfProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var employee = await GetEmployeeAsync(
            employeeId,
            asTracking: true,
            cancellationToken);
        try
        {
            SetExpectedRowVersion(employee, request.RowVersion);
        }
        catch (RbacOperationException exception)
        {
            StageAudit(
                employeeId,
                AuthorizationAuditActionTypes.EmployeeProfileUpdated,
                employeeId,
                null,
                AuthorizationAuditResultTypes.Denied,
                exception.Code);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }

        var fullName = NormalizeRequired(request.FullName, nameof(request.FullName));
        var birthDate = request.BirthDate?.Date;
        var gender = NormalizeOptional(request.Gender);
        var maritalStatus = NormalizeOptional(request.MaritalStatus);
        var mobile = NormalizeOptional(request.Mobile);
        var phone = NormalizeOptional(request.Phone);
        var email = NormalizeOptional(request.Email);
        var address = NormalizeOptional(request.Address);

        var changedFields = new List<string>();
        TrackChange(changedFields, "FullName", employee.EmployeeFullName, fullName);
        TrackChange(changedFields, "BirthDate", employee.EmployeeBirthDate?.Date, birthDate);
        TrackChange(changedFields, "Gender", employee.Gender, gender);
        TrackChange(changedFields, "MaritalStatus", employee.MaritalStatus, maritalStatus);
        TrackChange(changedFields, "Mobile", employee.EmployeeMobile, mobile);
        TrackChange(changedFields, "Phone", employee.EmployeePhone, phone);
        TrackChange(changedFields, "Email", employee.EmployeeEmail, email);
        TrackChange(changedFields, "Address", employee.EmployeeAddress, address);

        if (changedFields.Count > 0)
        {
            employee.EmployeeFullName = fullName;
            employee.EmployeeBirthDate = birthDate;
            employee.Gender = gender;
            employee.MaritalStatus = maritalStatus;
            employee.EmployeeMobile = mobile;
            employee.EmployeePhone = phone;
            employee.EmployeeEmail = email;
            employee.EmployeeAddress = address;
            employee.DateModified = DateTime.UtcNow;

            StageAudit(
                employeeId,
                AuthorizationAuditActionTypes.EmployeeProfileUpdated,
                employeeId,
                string.Join(',', changedFields));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var departmentName = await GetDepartmentNameAsync(
            employee.DepartmentId,
            cancellationToken);
        return Map(employee, departmentName);
    }

    public async Task ChangePasswordAsync(
        int employeeId,
        ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var employee = await GetEmployeeAsync(
            employeeId,
            asTracking: true,
            cancellationToken);

        try
        {
            AccountPasswordPolicy.VerifyCurrentPassword(
                _passwordHasher,
                employee,
                employee.EmployeePassword,
                request.CurrentPassword);
            AccountPasswordPolicy.EnsureNotReused(
                _passwordHasher,
                employee,
                employee.EmployeePassword,
                request.NewPassword);
        }
        catch (RbacOperationException exception)
        {
            StageAudit(
                employeeId,
                AuthorizationAuditActionTypes.EmployeePasswordChanged,
                employeeId,
                null,
                AuthorizationAuditResultTypes.Denied,
                exception.Code);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }

        employee.EmployeePassword = _passwordHasher.HashPassword(
            employee,
            request.NewPassword);
        employee.PasswordChangedAt = DateTime.UtcNow;
        employee.MustChangePassword = false;
        employee.SessionVersion = checked(Math.Max(1, employee.SessionVersion) + 1);
        employee.DateModified = DateTime.UtcNow;

        StageAudit(
            employeeId,
            AuthorizationAuditActionTypes.EmployeePasswordChanged,
            employeeId,
            null);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeProfileResponse> UploadProfileImageAsync(
        int employeeId,
        ProfileImageKind kind,
        ProfileImageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.File is null || request.File.Length <= 0)
        {
            throw new ArgumentException("Vui lòng chọn ảnh cần tải lên.");
        }

        var employee = await GetEmployeeAsync(
            employeeId,
            asTracking: true,
            cancellationToken);
        SetExpectedRowVersion(employee, request.RowVersion);

        var tenantCode = _currentTenant.GetRequiredTenant().TenantCode;
        var oldStorageKey = GetStorageKey(employee, kind);
        await using var content = request.File.OpenReadStream();
        var stored = await _privateFileStorage.SaveAsync(
            new PrivateFileSaveRequest(
                content,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                tenantCode,
                GetObjectType(kind),
                employee.EmployeeId,
                PrivateFileUploadPolicies.ProfileImage(GetMaximumSize(kind))),
            cancellationToken);

        ApplyStoredImage(employee, kind, stored);
        employee.DateModified = DateTime.UtcNow;
        StageAudit(
            employeeId,
            AuthorizationAuditActionTypes.EmployeeProfileUpdated,
            employeeId,
            GetChangedField(kind));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteStoredFileAsync(
                tenantCode,
                stored.StorageKey,
                cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldStorageKey))
        {
            await TryDeleteStoredFileAsync(
                tenantCode,
                oldStorageKey,
                cancellationToken);
        }

        var departmentName = await GetDepartmentNameAsync(
            employee.DepartmentId,
            cancellationToken);
        return Map(employee, departmentName);
    }

    public async Task<EmployeeProfileResponse> DeleteProfileImageAsync(
        int employeeId,
        ProfileImageKind kind,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeAsync(
            employeeId,
            asTracking: true,
            cancellationToken);
        SetExpectedRowVersion(employee, rowVersion);
        var oldStorageKey = GetStorageKey(employee, kind);

        if (!string.IsNullOrWhiteSpace(oldStorageKey))
        {
            ClearStoredImage(employee, kind);
            employee.DateModified = DateTime.UtcNow;
            StageAudit(
                employeeId,
                AuthorizationAuditActionTypes.EmployeeProfileUpdated,
                employeeId,
                GetChangedField(kind));
            await _dbContext.SaveChangesAsync(cancellationToken);

            await TryDeleteStoredFileAsync(
                _currentTenant.GetRequiredTenant().TenantCode,
                oldStorageKey,
                cancellationToken);
        }

        var departmentName = await GetDepartmentNameAsync(
            employee.DepartmentId,
            cancellationToken);
        return Map(employee, departmentName);
    }

    public async Task<ProfileImageFile> OpenProfileImageAsync(
        int employeeId,
        ProfileImageKind kind,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeAsync(
            employeeId,
            asTracking: false,
            cancellationToken);
        var storageKey = GetStorageKey(employee, kind);
        var contentType = GetContentType(employee, kind);
        if (string.IsNullOrWhiteSpace(storageKey)
            || string.IsNullOrWhiteSpace(contentType))
        {
            throw ProfileImageNotFound();
        }

        try
        {
            var stream = await _privateFileStorage.OpenReadAsync(
                _currentTenant.GetRequiredTenant().TenantCode,
                storageKey,
                cancellationToken);
            return new ProfileImageFile(stream, contentType);
        }
        catch (FileNotFoundException)
        {
            throw ProfileImageNotFound();
        }
    }

    private async Task<TblEmployee> GetEmployeeAsync(
        int employeeId,
        bool asTracking,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.TblEmployees.AsQueryable();
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        var employee = await query.FirstOrDefaultAsync(
            candidate => candidate.EmployeeId == employeeId,
            cancellationToken);
        if (employee is null || employee.Status != 1)
        {
            throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.EmployeeInactive,
                "Employee account is inactive or no longer exists.");
        }

        return employee;
    }

    private Task<string?> GetDepartmentNameAsync(
        int? departmentId,
        CancellationToken cancellationToken) =>
        departmentId.HasValue
            ? _dbContext.TblDepartments
                .AsNoTracking()
                .Where(department => department.DepartmentId == departmentId.Value)
                .Select(department => department.DepartmentName)
                .FirstOrDefaultAsync(cancellationToken)
            : Task.FromResult<string?>(null);

    private void SetExpectedRowVersion(TblEmployee employee, string rowVersion)
    {
        var expected = DecodeRowVersion(rowVersion);
        if (employee.RowVersion is not { Length: 8 }
            || !employee.RowVersion.AsSpan().SequenceEqual(expected))
        {
            throw new RbacOperationException(
                StatusCodes.Status409Conflict,
                AuthorizationErrorCodes.StaleRowVersion,
                "Hồ sơ đã được cập nhật bởi yêu cầu khác.");
        }

        _dbContext.Entry(employee)
            .Property(candidate => candidate.RowVersion)
            .OriginalValue = expected;
    }

    private void StageAudit(
        int actorEmployeeId,
        string action,
        int targetEmployeeId,
        string? changedFields,
        string result = AuthorizationAuditResultTypes.Success,
        string? failureCode = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        _dbContext.TblAuthorizationAudits.Add(
            AuthorizationAuditRecordFactory.CreateTenant(
                _currentTenant.GetRequiredTenant().TenantId,
                actorEmployeeId,
                "Employee",
                action,
                result,
                "Employee",
                targetEmployeeId.ToString(),
                null,
                null,
                null,
                null,
                failureCode,
                DateTime.UtcNow,
                httpContext?.Connection.RemoteIpAddress?.ToString(),
                httpContext?.Request.Headers.UserAgent.ToString(),
                httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
                changedFields));
    }

    private static EmployeeProfileResponse Map(
        TblEmployee employee,
        string? departmentName) =>
        new()
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            Account = employee.EmployeeAccount,
            FullName = employee.EmployeeFullName,
            BirthDate = employee.EmployeeBirthDate,
            Gender = employee.Gender,
            MaritalStatus = employee.MaritalStatus,
            Mobile = employee.EmployeeMobile,
            Phone = employee.EmployeePhone,
            Email = employee.EmployeeEmail,
            Address = employee.EmployeeAddress,
            DepartmentId = employee.DepartmentId,
            DepartmentName = departmentName,
            TitleId = employee.TitleId,
            TitleName = null,
            EmployeeType = employee.EmployeeType,
            RoleName = employee.EmployeeType.HasValue
                && Enum.IsDefined(typeof(EmployeeType), employee.EmployeeType.Value)
                    ? ((EmployeeType)employee.EmployeeType.Value).ToString()
                    : string.Empty,
            Status = employee.Status,
            ImageUrl = BuildImageUrl(
                employee.AvatarStorageKey,
                employee.AvatarUpdatedAt,
                "avatar"),
            CoverImageUrl = BuildImageUrl(
                employee.CoverStorageKey,
                employee.CoverUpdatedAt,
                "cover"),
            DefaultPage = employee.DefaultPage,
            MustChangePassword = employee.MustChangePassword,
            PasswordChangedAt = employee.PasswordChangedAt,
            RowVersion = employee.RowVersion is { Length: > 0 }
                ? Convert.ToBase64String(employee.RowVersion)
                : string.Empty
        };

    private static string? BuildImageUrl(
        string? storageKey,
        DateTime? updatedAt,
        string segment)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var version = updatedAt?.Ticks ?? 0;
        return $"/api/auth/profile/{segment}?v={version}";
    }

    private static string? GetStorageKey(
        TblEmployee employee,
        ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? employee.AvatarStorageKey
            : employee.CoverStorageKey;

    private static string? GetContentType(
        TblEmployee employee,
        ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? employee.AvatarContentType
            : employee.CoverContentType;

    private static void ApplyStoredImage(
        TblEmployee employee,
        ProfileImageKind kind,
        StoredPrivateFile stored)
    {
        if (kind == ProfileImageKind.Avatar)
        {
            employee.AvatarStorageKey = stored.StorageKey;
            employee.AvatarContentType = stored.ContentType;
            employee.AvatarFileSize = stored.FileSize;
            employee.AvatarSha256 = stored.Sha256;
            employee.AvatarUpdatedAt = stored.CreatedAt;
            return;
        }

        employee.CoverStorageKey = stored.StorageKey;
        employee.CoverContentType = stored.ContentType;
        employee.CoverFileSize = stored.FileSize;
        employee.CoverSha256 = stored.Sha256;
        employee.CoverUpdatedAt = stored.CreatedAt;
    }

    private static void ClearStoredImage(
        TblEmployee employee,
        ProfileImageKind kind)
    {
        if (kind == ProfileImageKind.Avatar)
        {
            employee.AvatarStorageKey = null;
            employee.AvatarContentType = null;
            employee.AvatarFileSize = null;
            employee.AvatarSha256 = null;
            employee.AvatarUpdatedAt = null;
            return;
        }

        employee.CoverStorageKey = null;
        employee.CoverContentType = null;
        employee.CoverFileSize = null;
        employee.CoverSha256 = null;
        employee.CoverUpdatedAt = null;
    }

    private async Task TryDeleteStoredFileAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _privateFileStorage.DeleteAsync(
                tenantCode,
                storageKey,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Không thể dọn profile image cũ {StorageKey} của tenant {TenantCode}.",
                storageKey,
                tenantCode);
        }
    }

    private static long GetMaximumSize(ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? 5 * 1024 * 1024
            : 8 * 1024 * 1024;

    private static string GetObjectType(ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? "EmployeeProfileAvatar"
            : "EmployeeProfileCover";

    private static string GetChangedField(ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar ? "Avatar" : "CoverImage";

    private static RbacOperationException ProfileImageNotFound() =>
        new(
            StatusCodes.Status404NotFound,
            AuthorizationErrorCodes.ResourceNotFound,
            "Không tìm thấy ảnh hồ sơ.");

    private static byte[] DecodeRowVersion(string rowVersion)
    {
        try
        {
            var bytes = Convert.FromBase64String(rowVersion);
            return bytes.Length == 8
                ? bytes
                : throw new FormatException();
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "RowVersion phải là Base64 hợp lệ của SQL rowversion.",
                nameof(rowVersion),
                exception);
        }
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} không được để trống.",
                parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void TrackChange<T>(
        ICollection<string> fields,
        string field,
        T oldValue,
        T newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            fields.Add(field);
        }
    }
}
