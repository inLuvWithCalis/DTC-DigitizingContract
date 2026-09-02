using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.DTOs.Requests.SystemAuthentication;
using ContractManagement.API.Domains.Interfaces.Authentication;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.API.Domains.Services.Security;
using ContractManagement.API.Domains.Services.SystemAuthentication;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ContractManagement.Tests.TestDoubles;

namespace ContractManagement.Tests.Domains.SystemAuthentication;

public sealed class SystemAdminSelfServiceTests
{
    private static readonly byte[] RowVersion = [8, 7, 6, 5, 4, 3, 2, 1];

    [Fact]
    public async Task Profile_UpdatesEditableFields_UsesRowVersion_AndAuditsFieldNames()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateAdmin();
        dbContext.SystemAdmins.Add(admin);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var profile = await service.GetProfileAsync(admin.SystemAdminId);
        Assert.Equal("root-admin", profile.Username);
        Assert.DoesNotContain(admin.PasswordHash, System.Text.Json.JsonSerializer.Serialize(profile));

        var updated = await service.UpdateProfileAsync(
            admin.SystemAdminId,
            new UpdateSystemAdminProfileRequest
            {
                FullName = "  Root Administrator  ",
                Email = "root@example.com",
                RowVersion = Convert.ToBase64String(RowVersion)
            });

        Assert.Equal("Root Administrator", updated.FullName);
        Assert.Equal("root-admin", admin.Username);
        Assert.True(admin.IsActive);
        var audit = await dbContext.SecurityAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditActionTypes.SystemAdminProfileUpdated, audit.Action);
        Assert.Equal("FullName,Email", audit.ChangedFields);
        Assert.DoesNotContain("root@example.com", audit.ChangedFields);
    }

    [Fact]
    public async Task Profile_StaleRowVersion_ReturnsStableConflict()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateAdmin();
        dbContext.SystemAdmins.Add(admin);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.UpdateProfileAsync(
                admin.SystemAdminId,
                new UpdateSystemAdminProfileRequest
                {
                    FullName = "Changed",
                    RowVersion = Convert.ToBase64String([1, 1, 1, 1, 1, 1, 1, 1])
                }));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.StaleRowVersion, exception.Code);
        var audit = await dbContext.SecurityAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditResultTypes.Denied, audit.Result);
        Assert.Equal(AuthorizationErrorCodes.StaleRowVersion, audit.FailureCode);
    }

    [Fact]
    public async Task Password_VerifiesCurrentRejectsReuse_AndInvalidatesSessionsWithoutAuditSecret()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateAdmin();
        var hasher = new PasswordHasher<SystemAdmin>();
        admin.PasswordHash = hasher.HashPassword(admin, "CurrentPassword123!");
        admin.MustChangePassword = true;
        dbContext.SystemAdmins.Add(admin);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, hasher);

        var wrong = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.ChangePasswordAsync(
                admin.SystemAdminId,
                new ChangeSystemAdminPasswordRequest
                {
                    CurrentPassword = "WrongPassword123!",
                    NewPassword = "AnotherPassword123!"
                }));
        Assert.Equal(AuthorizationErrorCodes.CurrentPasswordIncorrect, wrong.Code);

        var reused = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.ChangePasswordAsync(
                admin.SystemAdminId,
                new ChangeSystemAdminPasswordRequest
                {
                    CurrentPassword = "CurrentPassword123!",
                    NewPassword = "CurrentPassword123!"
                }));
        Assert.Equal(AuthorizationErrorCodes.PasswordReuseNotAllowed, reused.Code);

        await service.ChangePasswordAsync(
            admin.SystemAdminId,
            new ChangeSystemAdminPasswordRequest
            {
                CurrentPassword = "CurrentPassword123!",
                NewPassword = "NewSecurePassword123!"
            });

        Assert.False(admin.MustChangePassword);
        Assert.Equal(2, admin.SessionVersion);
        Assert.NotNull(admin.PasswordChangedAt);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                "NewSecurePassword123!"));
        var audits = await dbContext.SecurityAudits.ToListAsync();
        Assert.Equal(3, audits.Count);
        var audit = Assert.Single(
            audits,
            candidate => candidate.Result == AuthorizationAuditResultTypes.Success);
        Assert.Equal(AuthorizationAuditActionTypes.SystemAdminPasswordChanged, audit.Action);
        var serialized = System.Text.Json.JsonSerializer.Serialize(audits);
        Assert.DoesNotContain("CurrentPassword123!", serialized);
        Assert.DoesNotContain("NewSecurePassword123!", serialized);
        Assert.DoesNotContain(admin.PasswordHash, serialized);
    }

    [Fact]
    public async Task Authorize_RevalidatesSessionVersion_AndMustChangePassword()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateAdmin();
        dbContext.SystemAdmins.Add(admin);
        await dbContext.SaveChangesAsync();
        var writer = new CentralSecurityAuditWriter(
            dbContext,
            NullLogger<CentralSecurityAuditWriter>.Instance);
        var services = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<ICentralSecurityAuditWriter>(writer)
            .BuildServiceProvider();

        var oldSession = CreateSession(admin.SystemAdminId, 1);
        var allowed = CreateFilterContext(services, oldSession);
        await new SystemAdminAuthorizeAttribute().OnAuthorizationAsync(allowed);
        Assert.Null(allowed.Result);

        admin.SessionVersion = 2;
        await dbContext.SaveChangesAsync();
        var expired = CreateFilterContext(services, oldSession);
        await new SystemAdminAuthorizeAttribute().OnAuthorizationAsync(expired);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            Assert.IsType<ObjectResult>(expired.Result).StatusCode);
        Assert.Null(oldSession.GetInt32(AccountSessionKeys.SystemAdminId));

        admin.MustChangePassword = true;
        await dbContext.SaveChangesAsync();
        var currentSession = CreateSession(admin.SystemAdminId, 2);
        var forced = CreateFilterContext(services, currentSession);
        await new SystemAdminAuthorizeAttribute().OnAuthorizationAsync(forced);
        var forcedResult = Assert.IsType<ObjectResult>(forced.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forcedResult.StatusCode);
        Assert.Equal(
            AuthorizationErrorCodes.MustChangePassword,
            Assert.IsType<AuthorizationErrorResponse>(forcedResult.Value).Code);

        var selfService = CreateFilterContext(services, currentSession);
        await new SystemAdminAuthorizeAttribute
        {
            AllowWhenPasswordChangeRequired = true
        }.OnAuthorizationAsync(selfService);
        Assert.Null(selfService.Result);
    }

    [Fact]
    public async Task ProfileCover_UploadDownloadAndDelete_UsesCentralPrivateStorage()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateAdmin();
        dbContext.SystemAdmins.Add(admin);
        await dbContext.SaveChangesAsync();
        var storage = new InMemoryPrivateFileStorage();
        var service = CreateService(dbContext, storage: storage);

        var uploaded = await service.UploadProfileImageAsync(
            admin.SystemAdminId,
            ProfileImageKind.Cover,
            ImageRequest());

        Assert.Contains("/api/system-auth/profile/cover?v=", uploaded.CoverImageUrl);
        Assert.StartsWith("central/SystemAdminProfileCover/1/", admin.CoverStorageKey);
        Assert.Equal(8 * 1024 * 1024, storage.LastSaveRequest!.UploadPolicy.MaximumSizeBytes);

        await using (var stream = (await service.OpenProfileImageAsync(
            admin.SystemAdminId,
            ProfileImageKind.Cover)).Content)
        {
            Assert.True(stream.Length > 0);
        }

        var storageKey = admin.CoverStorageKey!;
        var deleted = await service.DeleteProfileImageAsync(
            admin.SystemAdminId,
            ProfileImageKind.Cover,
            uploaded.RowVersion);
        Assert.Null(deleted.CoverImageUrl);
        Assert.Contains(storageKey, storage.DeletedStorageKeys);
        Assert.Equal(2, await dbContext.SecurityAudits.CountAsync());
    }

    private static SystemAdminAccountService CreateService(
        CentralDbContext dbContext,
        IPasswordHasher<SystemAdmin>? hasher = null,
        InMemoryPrivateFileStorage? storage = null)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "system-admin-self-service-test"
        };
        return new SystemAdminAccountService(
            dbContext,
            hasher ?? new PasswordHasher<SystemAdmin>(),
            new CentralSecurityAuditWriter(
                dbContext,
                NullLogger<CentralSecurityAuditWriter>.Instance),
            new HttpContextAccessor { HttpContext = httpContext },
            storage ?? new InMemoryPrivateFileStorage(),
            NullLogger<SystemAdminAccountService>.Instance);
    }

    private static ProfileImageUploadRequest ImageRequest()
    {
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1
        };
        return new ProfileImageUploadRequest
        {
            File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "cover.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            },
            RowVersion = Convert.ToBase64String(RowVersion)
        };
    }

    private static SystemAdmin CreateAdmin() => new()
    {
        SystemAdminId = 1,
        Username = "root-admin",
        PasswordHash = "not-exposed-hash",
        FullName = "Root Admin",
        Email = "old@example.com",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        SessionVersion = 1,
        RowVersion = RowVersion.ToArray()
    };

    private static CentralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CentralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CentralDbContext(options);
    }

    private static TestSession CreateSession(int systemAdminId, int version)
    {
        var session = new TestSession();
        session.SetInt32(AccountSessionKeys.SystemAdminId, systemAdminId);
        session.SetInt32(AccountSessionKeys.SystemAdminSessionVersion, version);
        return session;
    }

    private static AuthorizationFilterContext CreateFilterContext(
        IServiceProvider services,
        ISession session)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Session = session
        };
        return new AuthorizationFilterContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()),
            []);
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _values.Keys;
        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, out byte[] value) =>
            _values.TryGetValue(key, out value!);
    }
}
