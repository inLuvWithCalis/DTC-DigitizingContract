using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.Services.Employee;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Domains.Security;

public sealed class SystemAdminManagerGovernanceServiceTests
{
    [Fact]
    public async Task SystemAdmin_CanAppointManager_AndWritesTenantAndCentralAudits()
    {
        var tenantDatabaseName = Guid.NewGuid().ToString();
        await SeedTenantAsync(tenantDatabaseName,
            NewEmployee(11, EmployeeType.Manager),
            NewEmployee(12, EmployeeType.Sale));
        await using var centralDbContext = CreateCentralDbContext();
        centralDbContext.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "system-admin",
            PasswordHash = "not-used",
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        centralDbContext.Tenants.Add(NewTenant());
        await centralDbContext.SaveChangesAsync();
        var service = CreateService(centralDbContext, tenantDatabaseName);

        await using var before = CreateTenantDbContext(tenantDatabaseName);
        var sale = await before.TblEmployees.SingleAsync(x => x.EmployeeId == 12);
        var response = await service.ChangeManagerRoleAsync(
            1,
            "tenant-a",
            12,
            new ChangeEmployeeRoleRequest
            {
                EmployeeType = (byte)EmployeeType.Manager,
                RowVersion = Convert.ToBase64String(sale.RowVersion)
            });

        Assert.Equal((byte)EmployeeType.Manager, response.EmployeeType);
        Assert.NotEmpty(response.RowVersion);

        await using var after = CreateTenantDbContext(tenantDatabaseName);
        var tenantAudit = await after.TblAuthorizationAudits.SingleAsync();
        Assert.Equal("ManagerRoleChanged", tenantAudit.Action);
        Assert.Equal((byte)EmployeeType.Sale, tenantAudit.PreviousEmployeeType);
        Assert.Equal((byte)EmployeeType.Manager, tenantAudit.NewEmployeeType);

        var centralAudit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal("ManagerRoleChanged", centralAudit.Action);
        Assert.Equal("Success", centralAudit.Result);
        Assert.Equal((byte)EmployeeType.Sale, centralAudit.PreviousEmployeeType);
        Assert.Equal((byte)EmployeeType.Manager, centralAudit.NewEmployeeType);
    }

    [Fact]
    public async Task SystemAdmin_CannotRevokeLastActiveManager()
    {
        var tenantDatabaseName = Guid.NewGuid().ToString();
        await SeedTenantAsync(tenantDatabaseName, NewEmployee(11, EmployeeType.Manager));
        await using var centralDbContext = CreateCentralDbContext();
        centralDbContext.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "system-admin",
            PasswordHash = "not-used",
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        centralDbContext.Tenants.Add(NewTenant());
        await centralDbContext.SaveChangesAsync();
        var service = CreateService(centralDbContext, tenantDatabaseName);

        await using var tenant = CreateTenantDbContext(tenantDatabaseName);
        var manager = await tenant.TblEmployees.SingleAsync();

        var exception = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.ChangeManagerRoleAsync(
                1,
                "tenant-a",
                11,
                new ChangeEmployeeRoleRequest
                {
                    EmployeeType = (byte)EmployeeType.Technical,
                    RowVersion = Convert.ToBase64String(manager.RowVersion)
                }));

        Assert.Equal(AuthorizationErrorCodes.LastActiveManager, exception.Code);
        var centralAudit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal("Denied", centralAudit.Result);
        Assert.Equal(AuthorizationErrorCodes.LastActiveManager, centralAudit.FailureCode);
    }

    [Fact]
    public async Task SystemAdmin_CanRevokeManager_WhenAnotherActiveManagerRemains()
    {
        var tenantDatabaseName = Guid.NewGuid().ToString();
        await SeedTenantAsync(
            tenantDatabaseName,
            NewEmployee(11, EmployeeType.Manager),
            NewEmployee(12, EmployeeType.Manager));
        await using var centralDbContext = CreateCentralDbContext();
        centralDbContext.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "system-admin",
            PasswordHash = "not-used",
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        centralDbContext.Tenants.Add(NewTenant());
        await centralDbContext.SaveChangesAsync();
        var service = CreateService(centralDbContext, tenantDatabaseName);

        await using var tenant = CreateTenantDbContext(tenantDatabaseName);
        var manager = await tenant.TblEmployees.SingleAsync(employee => employee.EmployeeId == 12);
        var response = await service.ChangeManagerRoleAsync(
            1,
            "tenant-a",
            12,
            new ChangeEmployeeRoleRequest
            {
                EmployeeType = (byte)EmployeeType.Technical,
                RowVersion = Convert.ToBase64String(manager.RowVersion)
            });

        Assert.Equal((byte)EmployeeType.Technical, response.EmployeeType);
        await using var after = CreateTenantDbContext(tenantDatabaseName);
        var remainingActiveManagerCount = await after.TblEmployees.CountAsync(
            employee => employee.Status == 1
                && employee.EmployeeType == (byte)EmployeeType.Manager);
        Assert.Equal(1, remainingActiveManagerCount);
        var centralAudit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal("ManagerRoleChanged", centralAudit.Action);
        Assert.Equal("Success", centralAudit.Result);
        Assert.Equal((byte)EmployeeType.Manager, centralAudit.PreviousEmployeeType);
        Assert.Equal((byte)EmployeeType.Technical, centralAudit.NewEmployeeType);
    }

    [Fact]
    public async Task SystemAdmin_RejectsStaleEmployeeRowVersion()
    {
        var tenantDatabaseName = Guid.NewGuid().ToString();
        await SeedTenantAsync(tenantDatabaseName,
            NewEmployee(11, EmployeeType.Manager),
            NewEmployee(12, EmployeeType.Sale));
        await using var centralDbContext = CreateCentralDbContext();
        centralDbContext.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "system-admin",
            PasswordHash = "not-used",
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        centralDbContext.Tenants.Add(NewTenant());
        await centralDbContext.SaveChangesAsync();
        var service = CreateService(centralDbContext, tenantDatabaseName);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.ChangeManagerRoleAsync(
                1,
                "tenant-a",
                12,
                new ChangeEmployeeRoleRequest
                {
                    EmployeeType = (byte)EmployeeType.Manager,
                    RowVersion = Convert.ToBase64String(new byte[8])
                }));

        Assert.Equal(AuthorizationErrorCodes.StaleRowVersion, exception.Code);
    }

    private static SystemAdminManagerGovernanceService CreateService(
        CentralDbContext centralDbContext,
        string tenantDatabaseName)
    {
        return new SystemAdminManagerGovernanceService(
            centralDbContext,
            new InMemoryTenantDbContextFactory(tenantDatabaseName),
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "manager-governance-test"
                }
            });
    }

    private static async Task SeedTenantAsync(
        string databaseName,
        params TblEmployee[] employees)
    {
        await using var context = CreateTenantDbContext(databaseName);
        context.TblEmployees.AddRange(employees);
        await context.SaveChangesAsync();
    }

    private static DbDtctechContext CreateTenantDbContext(string databaseName)
    {
        return new DbDtctechContext(
            new DbContextOptionsBuilder<DbDtctechContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
    }

    private static CentralDbContext CreateCentralDbContext()
    {
        return new CentralDbContext(
            new DbContextOptionsBuilder<CentralDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static Tenant NewTenant() => new()
    {
        TenantId = 101,
        TenantCode = "tenant-a",
        TenantName = "Tenant A",
        Status = TenantStatus.Active,
        CreatedAt = DateTime.UtcNow,
        TenantDatabase = new TenantDatabase
        {
            TenantDatabaseId = 301,
            DatabaseKey = "dedicated-tenant-a",
            DatabaseName = "not-exposed",
            ConnectionString = "tenant-a",
            Mode = TenantDatabaseMode.Dedicated,
            CreatedAt = DateTime.UtcNow
        }
    };

    private static TblEmployee NewEmployee(int id, EmployeeType type) => new()
    {
        EmployeeId = id,
        EmployeeAccount = $"employee-{id}",
        EmployeeFullName = $"Employee {id}",
        EmployeeType = (byte)type,
        Status = 1
    };

    private sealed class InMemoryTenantDbContextFactory : ITenantDbContextFactory
    {
        private readonly string _databaseName;

        public InMemoryTenantDbContextFactory(string databaseName)
        {
            _databaseName = databaseName;
        }

        public DbDtctechContext Create(string connectionString) =>
            CreateTenantDbContext(_databaseName);
    }
}
