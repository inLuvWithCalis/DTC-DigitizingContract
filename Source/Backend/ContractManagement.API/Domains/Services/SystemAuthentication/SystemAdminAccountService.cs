using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.SystemAuthentication;
using ContractManagement.API.Domains.DTOs.Responses.SystemAuthentication;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.API.Domains.Interfaces.SystemAuthentication;
using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.Interfaces.Authentication;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.File;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.SystemAuthentication;

public sealed class SystemAdminAccountService : ISystemAdminAccountService
{
    private readonly CentralDbContext _dbContext;
    private readonly IPasswordHasher<SystemAdmin> _passwordHasher;
    private readonly ICentralSecurityAuditWriter _auditWriter;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPrivateFileStorage _privateFileStorage;
    private readonly ILogger<SystemAdminAccountService> _logger;

    private const string CentralStorageScope = "central";

    public SystemAdminAccountService(
        CentralDbContext dbContext,
        IPasswordHasher<SystemAdmin> passwordHasher,
        ICentralSecurityAuditWriter auditWriter,
        IHttpContextAccessor httpContextAccessor,
        IPrivateFileStorage privateFileStorage,
        ILogger<SystemAdminAccountService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _auditWriter = auditWriter;
        _httpContextAccessor = httpContextAccessor;
        _privateFileStorage = privateFileStorage;
        _logger = logger;
    }

    public async Task<SystemAdminProfileResponse> GetProfileAsync(
        int systemAdminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await GetAdminAsync(
            systemAdminId,
            asTracking: false,
            cancellationToken);
        return Map(admin);
    }

    public async Task<SystemAdminProfileResponse> UpdateProfileAsync(
        int systemAdminId,
        UpdateSystemAdminProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var admin = await GetAdminAsync(
            systemAdminId,
            asTracking: true,
            cancellationToken);
        try
        {
            SetExpectedRowVersion(admin, request.RowVersion);
        }
        catch (RbacOperationException exception)
        {
            await WriteAuditAsync(
                systemAdminId,
                AuthorizationAuditActionTypes.SystemAdminProfileUpdated,
                null,
                cancellationToken,
                AuthorizationAuditResultTypes.Denied,
                exception.Code);
            throw;
        }

        var fullName = NormalizeRequired(request.FullName, nameof(request.FullName));
        var email = NormalizeOptional(request.Email);
        var changedFields = new List<string>();
        if (!string.Equals(admin.FullName, fullName, StringComparison.Ordinal))
        {
            changedFields.Add("FullName");
        }
        if (!string.Equals(admin.Email, email, StringComparison.Ordinal))
        {
            changedFields.Add("Email");
        }

        if (changedFields.Count > 0)
        {
            admin.FullName = fullName;
            admin.Email = email;
            admin.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                systemAdminId,
                AuthorizationAuditActionTypes.SystemAdminProfileUpdated,
                string.Join(',', changedFields),
                cancellationToken);
        }

        return Map(admin);
    }

    public async Task ChangePasswordAsync(
        int systemAdminId,
        ChangeSystemAdminPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var admin = await GetAdminAsync(
            systemAdminId,
            asTracking: true,
            cancellationToken);

        try
        {
            AccountPasswordPolicy.VerifyCurrentPassword(
                _passwordHasher,
                admin,
                admin.PasswordHash,
                request.CurrentPassword);
            AccountPasswordPolicy.EnsureNotReused(
                _passwordHasher,
                admin,
                admin.PasswordHash,
                request.NewPassword);
        }
        catch (RbacOperationException exception)
        {
            await WriteAuditAsync(
                systemAdminId,
                AuthorizationAuditActionTypes.SystemAdminPasswordChanged,
                null,
                cancellationToken,
                AuthorizationAuditResultTypes.Denied,
                exception.Code);
            throw;
        }

        admin.PasswordHash = _passwordHasher.HashPassword(
            admin,
            request.NewPassword);
        admin.PasswordChangedAt = DateTime.UtcNow;
        admin.MustChangePassword = false;
        admin.SessionVersion = checked(Math.Max(1, admin.SessionVersion) + 1);
        admin.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(
            systemAdminId,
            AuthorizationAuditActionTypes.SystemAdminPasswordChanged,
            null,
            cancellationToken);
    }

    public async Task<SystemAdminProfileResponse> UploadProfileImageAsync(
        int systemAdminId,
        ProfileImageKind kind,
        ProfileImageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.File is null || request.File.Length <= 0)
        {
            throw new ArgumentException("Vui lòng chọn ảnh cần tải lên.");
        }

        var admin = await GetAdminAsync(
            systemAdminId,
            asTracking: true,
            cancellationToken);
        SetExpectedRowVersion(admin, request.RowVersion);
        var oldStorageKey = GetStorageKey(admin, kind);

        await using var content = request.File.OpenReadStream();
        var stored = await _privateFileStorage.SaveAsync(
            new PrivateFileSaveRequest(
                content,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                CentralStorageScope,
                GetObjectType(kind),
                admin.SystemAdminId,
                PrivateFileUploadPolicies.ProfileImage(GetMaximumSize(kind))),
            cancellationToken);

        ApplyStoredImage(admin, kind, stored);
        admin.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteStoredFileAsync(stored.StorageKey, cancellationToken);
            throw;
        }

        await WriteAuditAsync(
            systemAdminId,
            AuthorizationAuditActionTypes.SystemAdminProfileUpdated,
            GetChangedField(kind),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldStorageKey))
        {
            await TryDeleteStoredFileAsync(oldStorageKey, cancellationToken);
        }

        return Map(admin);
    }

    public async Task<SystemAdminProfileResponse> DeleteProfileImageAsync(
        int systemAdminId,
        ProfileImageKind kind,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        var admin = await GetAdminAsync(
            systemAdminId,
            asTracking: true,
            cancellationToken);
        SetExpectedRowVersion(admin, rowVersion);
        var oldStorageKey = GetStorageKey(admin, kind);

        if (!string.IsNullOrWhiteSpace(oldStorageKey))
        {
            ClearStoredImage(admin, kind);
            admin.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                systemAdminId,
                AuthorizationAuditActionTypes.SystemAdminProfileUpdated,
                GetChangedField(kind),
                cancellationToken);
            await TryDeleteStoredFileAsync(oldStorageKey, cancellationToken);
        }

        return Map(admin);
    }

    public async Task<ProfileImageFile> OpenProfileImageAsync(
        int systemAdminId,
        ProfileImageKind kind,
        CancellationToken cancellationToken = default)
    {
        var admin = await GetAdminAsync(
            systemAdminId,
            asTracking: false,
            cancellationToken);
        var storageKey = GetStorageKey(admin, kind);
        var contentType = GetContentType(admin, kind);
        if (string.IsNullOrWhiteSpace(storageKey)
            || string.IsNullOrWhiteSpace(contentType))
        {
            throw ProfileImageNotFound();
        }

        try
        {
            var stream = await _privateFileStorage.OpenReadAsync(
                CentralStorageScope,
                storageKey,
                cancellationToken);
            return new ProfileImageFile(stream, contentType);
        }
        catch (FileNotFoundException)
        {
            throw ProfileImageNotFound();
        }
    }

    private async Task<SystemAdmin> GetAdminAsync(
        int systemAdminId,
        bool asTracking,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.SystemAdmins.AsQueryable();
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        var admin = await query.FirstOrDefaultAsync(
            candidate => candidate.SystemAdminId == systemAdminId,
            cancellationToken);
        if (admin is null || !admin.IsActive)
        {
            throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "System Admin session is no longer valid.");
        }

        return admin;
    }

    private void SetExpectedRowVersion(SystemAdmin admin, string rowVersion)
    {
        var expected = DecodeRowVersion(rowVersion);
        if (admin.RowVersion is not { Length: 8 }
            || !admin.RowVersion.AsSpan().SequenceEqual(expected))
        {
            throw new RbacOperationException(
                StatusCodes.Status409Conflict,
                AuthorizationErrorCodes.StaleRowVersion,
                "Hồ sơ đã được cập nhật bởi yêu cầu khác.");
        }

        _dbContext.Entry(admin)
            .Property(candidate => candidate.RowVersion)
            .OriginalValue = expected;
    }

    private Task WriteAuditAsync(
        int systemAdminId,
        string action,
        string? changedFields,
        CancellationToken cancellationToken,
        string result = AuthorizationAuditResultTypes.Success,
        string? failureCode = null)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "HTTP context is required for account security audit.");
        return _auditWriter.TryWriteAsync(
            httpContext,
            new CentralSecurityAuditWriteRequest(
                systemAdminId,
                null,
                null,
                action,
                result,
                "SystemAdmin",
                systemAdminId.ToString(),
                failureCode,
                ChangedFields: changedFields),
            cancellationToken);
    }

    private static SystemAdminProfileResponse Map(SystemAdmin admin) =>
        new()
        {
            SystemAdminId = admin.SystemAdminId,
            Username = admin.Username,
            FullName = admin.FullName,
            Email = admin.Email,
            IsActive = admin.IsActive,
            MustChangePassword = admin.MustChangePassword,
            PasswordChangedAt = admin.PasswordChangedAt,
            ImageUrl = BuildImageUrl(
                admin.AvatarStorageKey,
                admin.AvatarUpdatedAt,
                "avatar"),
            CoverImageUrl = BuildImageUrl(
                admin.CoverStorageKey,
                admin.CoverUpdatedAt,
                "cover"),
            RowVersion = admin.RowVersion is { Length: > 0 }
                ? Convert.ToBase64String(admin.RowVersion)
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
        return $"/api/system-auth/profile/{segment}?v={version}";
    }

    private static string? GetStorageKey(
        SystemAdmin admin,
        ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? admin.AvatarStorageKey
            : admin.CoverStorageKey;

    private static string? GetContentType(
        SystemAdmin admin,
        ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? admin.AvatarContentType
            : admin.CoverContentType;

    private static void ApplyStoredImage(
        SystemAdmin admin,
        ProfileImageKind kind,
        StoredPrivateFile stored)
    {
        if (kind == ProfileImageKind.Avatar)
        {
            admin.AvatarStorageKey = stored.StorageKey;
            admin.AvatarContentType = stored.ContentType;
            admin.AvatarFileSize = stored.FileSize;
            admin.AvatarSha256 = stored.Sha256;
            admin.AvatarUpdatedAt = stored.CreatedAt;
            return;
        }

        admin.CoverStorageKey = stored.StorageKey;
        admin.CoverContentType = stored.ContentType;
        admin.CoverFileSize = stored.FileSize;
        admin.CoverSha256 = stored.Sha256;
        admin.CoverUpdatedAt = stored.CreatedAt;
    }

    private static void ClearStoredImage(
        SystemAdmin admin,
        ProfileImageKind kind)
    {
        if (kind == ProfileImageKind.Avatar)
        {
            admin.AvatarStorageKey = null;
            admin.AvatarContentType = null;
            admin.AvatarFileSize = null;
            admin.AvatarSha256 = null;
            admin.AvatarUpdatedAt = null;
            return;
        }

        admin.CoverStorageKey = null;
        admin.CoverContentType = null;
        admin.CoverFileSize = null;
        admin.CoverSha256 = null;
        admin.CoverUpdatedAt = null;
    }

    private async Task TryDeleteStoredFileAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _privateFileStorage.DeleteAsync(
                CentralStorageScope,
                storageKey,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Không thể dọn profile image cũ {StorageKey} của System Admin.",
                storageKey);
        }
    }

    private static long GetMaximumSize(ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? 5 * 1024 * 1024
            : 8 * 1024 * 1024;

    private static string GetObjectType(ProfileImageKind kind) =>
        kind == ProfileImageKind.Avatar
            ? "SystemAdminProfileAvatar"
            : "SystemAdminProfileCover";

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
}
