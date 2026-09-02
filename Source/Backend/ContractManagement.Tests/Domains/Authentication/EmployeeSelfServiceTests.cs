using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.Interfaces.Authentication;
using ContractManagement.API.Domains.Services.Authentication;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ContractManagement.Tests.TestDoubles;

namespace ContractManagement.Tests.Domains.Authentication;

public sealed class EmployeeSelfServiceTests
{
    private static readonly byte[] RowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Profile_UpdateOnlySelfServiceFields_AndWritesSafeAudit()
    {
        await using var dbContext = CreateDbContext();
        var employee = CreateEmployee();
        dbContext.TblEmployees.Add(employee);
        dbContext.TblDepartments.Add(new TblDepartment
        {
            DepartmentId = 9,
            DepartmentCode = "SALE",
            DepartmentName = "Kinh doanh"
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var profile = await service.GetProfileAsync(employee.EmployeeId);
        Assert.Equal("sale-user", profile.Account);
        Assert.Equal("Kinh doanh", profile.DepartmentName);
        Assert.DoesNotContain("old-password", System.Text.Json.JsonSerializer.Serialize(profile));

        var updated = await service.UpdateProfileAsync(
            employee.EmployeeId,
            new UpdateEmployeeSelfProfileRequest
            {
                FullName = "  Nguyễn Văn Sale  ",
                Mobile = "0901000000",
                Email = "sale@example.com",
                Address = "Hà Nội",
                RowVersion = Convert.ToBase64String(RowVersion)
            });

        Assert.Equal("Nguyễn Văn Sale", updated.FullName);
        var persisted = await dbContext.TblEmployees.SingleAsync();
        Assert.Equal("sale-user", persisted.EmployeeAccount);
        Assert.Equal((byte)EmployeeType.Sale, persisted.EmployeeType);
        Assert.Equal(9, persisted.DepartmentId);
        var audit = await dbContext.TblAuthorizationAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditActionTypes.EmployeeProfileUpdated, audit.Action);
        Assert.Contains("FullName", audit.ChangedFields);
        Assert.Contains("Mobile", audit.ChangedFields);
        Assert.DoesNotContain("sale@example.com", audit.ChangedFields);
    }

    [Fact]
    public async Task Profile_StaleRowVersion_ReturnsStableConflict()
    {
        await using var dbContext = CreateDbContext();
        var employee = CreateEmployee();
        dbContext.TblEmployees.Add(employee);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.UpdateProfileAsync(
                employee.EmployeeId,
                new UpdateEmployeeSelfProfileRequest
                {
                    FullName = "Changed",
                    RowVersion = Convert.ToBase64String([8, 7, 6, 5, 4, 3, 2, 1])
                }));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.StaleRowVersion, exception.Code);
        var audit = await dbContext.TblAuthorizationAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditResultTypes.Denied, audit.Result);
        Assert.Equal(AuthorizationErrorCodes.StaleRowVersion, audit.FailureCode);
    }

    [Fact]
    public async Task Password_RequiresCurrentPolicyAndNoReuse_ThenInvalidatesSessions()
    {
        await using var dbContext = CreateDbContext();
        var employee = CreateEmployee();
        var hasher = new PasswordHasher<TblEmployee>();
        employee.EmployeePassword = hasher.HashPassword(employee, "CurrentPassword123!");
        employee.MustChangePassword = true;
        dbContext.TblEmployees.Add(employee);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, hasher);

        var wrong = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.ChangePasswordAsync(
                employee.EmployeeId,
                new ChangeOwnPasswordRequest
                {
                    CurrentPassword = "WrongPassword123!",
                    NewPassword = "AnotherPassword123!"
                }));
        Assert.Equal(AuthorizationErrorCodes.CurrentPasswordIncorrect, wrong.Code);

        var weak = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.ChangePasswordAsync(
                employee.EmployeeId,
                new ChangeOwnPasswordRequest
                {
                    CurrentPassword = "CurrentPassword123!",
                    NewPassword = "short"
                }));
        Assert.Equal(AuthorizationErrorCodes.PasswordPolicyViolation, weak.Code);

        var reused = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.ChangePasswordAsync(
                employee.EmployeeId,
                new ChangeOwnPasswordRequest
                {
                    CurrentPassword = "CurrentPassword123!",
                    NewPassword = "CurrentPassword123!"
                }));
        Assert.Equal(AuthorizationErrorCodes.PasswordReuseNotAllowed, reused.Code);

        await service.ChangePasswordAsync(
            employee.EmployeeId,
            new ChangeOwnPasswordRequest
            {
                CurrentPassword = "CurrentPassword123!",
                NewPassword = "NewSecurePassword123!"
            });

        var persisted = await dbContext.TblEmployees.SingleAsync();
        Assert.False(persisted.MustChangePassword);
        Assert.Equal(2, persisted.SessionVersion);
        Assert.NotNull(persisted.PasswordChangedAt);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(
                persisted,
                persisted.EmployeePassword!,
                "NewSecurePassword123!"));
        var audits = await dbContext.TblAuthorizationAudits.ToListAsync();
        Assert.Equal(4, audits.Count);
        var audit = Assert.Single(
            audits,
            candidate => candidate.Result == AuthorizationAuditResultTypes.Success);
        Assert.Equal(AuthorizationAuditActionTypes.EmployeePasswordChanged, audit.Action);
        var serialized = System.Text.Json.JsonSerializer.Serialize(audits);
        Assert.DoesNotContain("CurrentPassword123!", serialized);
        Assert.DoesNotContain("NewSecurePassword123!", serialized);
        Assert.DoesNotContain(persisted.EmployeePassword!, serialized);
    }

    [Fact]
    public async Task ProfileImage_UploadDownloadReplaceAndDelete_UsesTenantPrivateStorage()
    {
        await using var dbContext = CreateDbContext();
        var employee = CreateEmployee();
        dbContext.TblEmployees.Add(employee);
        await dbContext.SaveChangesAsync();
        var storage = new InMemoryPrivateFileStorage();
        var service = CreateService(dbContext, storage: storage);

        var first = await service.UploadProfileImageAsync(
            employee.EmployeeId,
            ProfileImageKind.Avatar,
            ImageRequest("avatar.png"));

        Assert.Contains("/api/auth/profile/avatar?v=", first.ImageUrl);
        Assert.StartsWith("tenant-a/EmployeeProfileAvatar/11/", employee.AvatarStorageKey);
        Assert.Equal(5 * 1024 * 1024, storage.LastSaveRequest!.UploadPolicy.MaximumSizeBytes);

        await using (var stream = (await service.OpenProfileImageAsync(
            employee.EmployeeId,
            ProfileImageKind.Avatar)).Content)
        {
            Assert.True(stream.Length > 0);
        }

        var previousStorageKey = employee.AvatarStorageKey!;
        await service.UploadProfileImageAsync(
            employee.EmployeeId,
            ProfileImageKind.Avatar,
            ImageRequest("replacement.jpg", "image/jpeg", first.RowVersion));
        Assert.Contains(previousStorageKey, storage.DeletedStorageKeys);

        var replaced = await service.GetProfileAsync(employee.EmployeeId);
        var deleted = await service.DeleteProfileImageAsync(
            employee.EmployeeId,
            ProfileImageKind.Avatar,
            replaced.RowVersion);
        Assert.Null(deleted.ImageUrl);
        Assert.Null(employee.AvatarStorageKey);
        Assert.Equal(3, await dbContext.TblAuthorizationAudits.CountAsync());
    }

    private static EmployeeAccountService CreateService(
        DbDtctechContext dbContext,
        IPasswordHasher<TblEmployee>? hasher = null,
        InMemoryPrivateFileStorage? storage = null)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            101,
            "tenant-a",
            "Tenant A",
            TenantDatabaseMode.Dedicated,
            "unused"));
        return new EmployeeAccountService(
            dbContext,
            hasher ?? new PasswordHasher<TblEmployee>(),
            tenant,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "employee-self-service-test"
                }
            },
            storage ?? new InMemoryPrivateFileStorage(),
            NullLogger<EmployeeAccountService>.Instance);
    }

    private static ProfileImageUploadRequest ImageRequest(
        string fileName,
        string contentType = "image/png",
        string? rowVersion = null)
    {
        var bytes = contentType == "image/png"
            ? new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1 }
            : new byte[] { 0xFF, 0xD8, 0xFF, 1 };
        return new ProfileImageUploadRequest
        {
            File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            },
            RowVersion = rowVersion ?? Convert.ToBase64String(RowVersion)
        };
    }

    private static TblEmployee CreateEmployee() => new()
    {
        EmployeeId = 11,
        EmployeeCode = "NV011",
        EmployeeAccount = "sale-user",
        EmployeePassword = "old-password-hash",
        EmployeeFullName = "Sale User",
        DepartmentId = 9,
        EmployeeType = (byte)EmployeeType.Sale,
        Status = 1,
        SessionVersion = 1,
        RowVersion = RowVersion.ToArray()
    };

    private static DbDtctechContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }
}
