using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Responses.Authentication;
using ContractManagement.Domains.Controllers.Authentication;
using ContractManagement.Domains.DTOs.Requests.Authentication;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Domains.Security;

public sealed class AuthControllerRbacTests
{
    [Fact]
    public async Task Login_WithInactiveEmployee_ReturnsEmployeeInactive()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new PasswordHasher<TblEmployee>();
        var employee = new TblEmployee
        {
            EmployeeId = 21,
            EmployeeAccount = "inactive",
            EmployeeFullName = "Inactive Employee",
            EmployeeType = (byte)EmployeeType.Sale,
            Status = 0
        };
        employee.EmployeePassword = hasher.HashPassword(employee, "Password123!");
        dbContext.TblEmployees.Add(employee);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, hasher);

        var result = await controller.Login(
            "tenant-a",
            new LoginRequest
            {
                AccountName = "inactive",
                Password = "Password123!"
            });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(
            AuthorizationErrorCodes.EmployeeInactive,
            Assert.IsType<AuthorizationErrorResponse>(unauthorized.Value).Code);
    }

    [Fact]
    public void Me_ReturnsAllowlistPermissionContract()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(
            dbContext,
            new PasswordHasher<TblEmployee>());
        var permissions = EmployeePermissionCatalog.GetPermissions(EmployeeType.Manager);
        controller.HttpContext.Items[EmployeeAuthorizationContext.HttpContextItemKey] =
            new AuthenticatedEmployee(
                22,
                "manager",
                "Tenant Manager",
                EmployeeType.Manager,
                permissions);

        var result = controller.GetCurrentUsers();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthMeResponse>(ok.Value);
        Assert.Equal(22, response.EmployeeId);
        Assert.Equal("manager", response.Account);
        Assert.Equal(nameof(EmployeeType.Manager), response.RoleName);
        Assert.Equal(RbacPermissions.Version, response.PermissionVersion);
        Assert.Equal(permissions, response.Permissions);
        Assert.IsNotType<TblEmployee>(ok.Value);
    }

    private static DbDtctechContext CreateDbContext()
    {
        return new DbDtctechContext(
            new DbContextOptionsBuilder<DbDtctechContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static AuthController CreateController(
        DbDtctechContext dbContext,
        IPasswordHasher<TblEmployee> hasher)
    {
        var currentTenant = new CurrentTenant();
        currentTenant.Set(new ResolvedTenant(
            101,
            "tenant-a",
            "Tenant A",
            TenantDatabaseMode.Dedicated,
            "unused"));

        return new AuthController(dbContext, hasher, currentTenant)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new TestSession()
                }
            }
        };
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

        public bool TryGetValue(string key, out byte[] value)
        {
            return _values.TryGetValue(key, out value!);
        }
    }
}
