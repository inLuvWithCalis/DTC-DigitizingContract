using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.Services.Employee;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Domains.Security;

public sealed class EmployeeGovernanceServiceTests
{
    [Fact]
    public async Task Manager_CannotCreateManagerOrModifyExistingManager()
    {
        await using var dbContext = CreateDbContext();
        var manager = NewEmployee(1, EmployeeType.Manager);
        var otherManager = NewEmployee(2, EmployeeType.Manager);
        dbContext.TblEmployees.AddRange(manager, otherManager);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var createException = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.CreateManagedEmployeeAsync(1, new CreateEmployeeRequest
            {
                EmployeeAccount = "new-manager",
                EmployeePassword = "Password123!",
                EmployeeFullName = "New Manager",
                EmployeeType = (byte)EmployeeType.Manager
            }));

        var updateException = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.UpdateManagedEmployeeAsync(1, 2, new UpdateEmployeeRequest
            {
                EmployeeFullName = "Other Manager",
                EmployeeType = (byte)EmployeeType.Manager,
                RowVersion = Convert.ToBase64String(otherManager.RowVersion)
            }));

        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, createException.Code);
        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, updateException.Code);
    }

    [Fact]
    public async Task Manager_StatusChange_UsesRowVersionAndWritesTenantAudit()
    {
        await using var dbContext = CreateDbContext();
        var manager = NewEmployee(1, EmployeeType.Manager);
        var employee = NewEmployee(2, EmployeeType.Sale);
        dbContext.TblEmployees.AddRange(manager, employee);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.SetManagedEmployeeStatusAsync(1, 2,
            new SetEmployeeStatusRequest
            {
                Status = 0,
                RowVersion = Convert.ToBase64String(employee.RowVersion)
            });

        var audit = await dbContext.TblAuthorizationAudits.SingleAsync();
        Assert.Equal("EmployeeStatusChanged", audit.Action);
        Assert.Equal((byte)1, audit.PreviousStatus!.Value);
        Assert.Equal((byte)0, audit.NewStatus!.Value);
        Assert.Equal(1, audit.ActorEmployeeId);
        Assert.Equal(2.ToString(), audit.TargetId);

        var staleException = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.SetManagedEmployeeStatusAsync(1, 2,
                new SetEmployeeStatusRequest
                {
                    Status = 1,
                    RowVersion = Convert.ToBase64String(new byte[8])
                }));
        Assert.Equal(AuthorizationErrorCodes.StaleRowVersion, staleException.Code);
    }

    [Fact]
    public async Task Directory_ReturnsOnlyActiveEmployeesWithValidFixedTypes()
    {
        await using var dbContext = CreateDbContext();
        dbContext.TblEmployees.AddRange(
            NewEmployee(1, EmployeeType.Manager),
            NewEmployee(2, EmployeeType.Sale),
            NewEmployee(3, EmployeeType.Technical, status: 0),
            new TblEmployee
            {
                EmployeeId = 4,
                EmployeeAccount = "legacy",
                EmployeeFullName = "Legacy Invalid Role",
                EmployeeType = null,
                Status = 1
            });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var directory = await service.GetDirectoryAsync();

        Assert.Equal(new[] { 1, 2 }, directory.Select(x => x.EmployeeId).Order());
        Assert.All(directory, item => Assert.Equal((byte)1, item.Status));

        var search = await service.SearchDirectoryAsync(
            new EmployeeDirectoryFilterRequest
            {
                Keyword = "Employee 2",
                Page = 1,
                PageSize = 10
            });
        var matched = Assert.Single(search.Items);
        Assert.Equal(2, matched.EmployeeId);
        Assert.Equal(1, search.TotalCount);
    }

    [Fact]
    public async Task AuthorizationAudit_IsAppendOnlyAtEfLevel()
    {
        await using var dbContext = CreateDbContext();
        var audit = new TblAuthorizationAudit
        {
            TenantId = 101,
            ActorType = "Employee",
            Action = "EmployeeCreated",
            Result = "Success",
            TargetType = "Employee",
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "test"
        };
        dbContext.TblAuthorizationAudits.Add(audit);
        await dbContext.SaveChangesAsync();

        audit.Action = "Changed";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task PasswordReset_AuditDoesNotContainTheNewPasswordOrHash()
    {
        await using var dbContext = CreateDbContext();
        var manager = NewEmployee(1, EmployeeType.Manager);
        var employee = NewEmployee(2, EmployeeType.Sale);
        dbContext.TblEmployees.AddRange(manager, employee);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        const string newPassword = "NewPassword123!";

        await service.ResetManagedEmployeePasswordAsync(1, 2,
            new ChangePasswordRequest
            {
                NewPassword = newPassword,
                RowVersion = Convert.ToBase64String(employee.RowVersion)
            });

        Assert.True(employee.MustChangePassword);
        Assert.Equal(2, employee.SessionVersion);
        Assert.NotNull(employee.PasswordChangedAt);
        var audit = await dbContext.TblAuthorizationAudits.SingleAsync();
        Assert.Equal(
            AuthorizationAuditActionTypes.EmployeePasswordResetByManager,
            audit.Action);
        Assert.Equal(1, audit.ActorEmployeeId);
        Assert.Equal(2.ToString(), audit.TargetId);
        Assert.DoesNotContain(newPassword, new[]
        {
            audit.TargetId,
            audit.FailureCode,
            audit.CorrelationId,
            audit.UserAgent
        });
        Assert.DoesNotContain(
            typeof(TblAuthorizationAudit).GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    private static DbDtctechContext CreateDbContext()
    {
        return new DbDtctechContext(
            new DbContextOptionsBuilder<DbDtctechContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static EmployeeService CreateService(DbDtctechContext dbContext)
    {
        var currentTenant = new CurrentTenant();
        currentTenant.Set(new ResolvedTenant(
            101,
            "tenant-a",
            "Tenant A",
            TenantDatabaseMode.Dedicated,
            "unused"));
        return new EmployeeService(
            dbContext,
            new PasswordHasher<TblEmployee>(),
            currentTenant,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "employee-governance-test"
                }
            });
    }

    private static TblEmployee NewEmployee(
        int employeeId,
        EmployeeType employeeType,
        byte status = 1)
    {
        return new TblEmployee
        {
            EmployeeId = employeeId,
            EmployeeAccount = $"employee-{employeeId}",
            EmployeeFullName = $"Employee {employeeId}",
            EmployeeType = (byte)employeeType,
            Status = status
        };
    }
}
