using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.Controllers.Security;
using ContractManagement.API.Domains.DTOs.Requests.Security;
using ContractManagement.API.Domains.DTOs.Responses.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.API.Domains.Services.Security;
using ContractManagement.Controllers.Admin;
using ContractManagement.Domains.Controllers.SystemAuth;
using ContractManagement.Domains.DTOs.Requests.SystemAuth;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using ContractManagement.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace ContractManagement.Tests.Domains.Security;

public sealed class SecurityAuditSlice05Tests
{
    [Fact]
    public async Task TenantAuditQuery_IsManagerOnlyAndAlwaysScopedToCurrentTenant()
    {
        await using var dbContext = CreateTenantDbContext();
        dbContext.TblEmployees.AddRange(
            NewEmployee(1, EmployeeType.Manager),
            NewEmployee(2, EmployeeType.Sale));
        dbContext.TblAuthorizationAudits.AddRange(
            TenantAudit(101, 1, "EmployeeStatusChanged"),
            TenantAudit(202, 2, "AccessDenied"));
        await dbContext.SaveChangesAsync();

        var service = new TenantSecurityAuditQueryService(
            dbContext,
            CurrentTenant(101, "tenant-a"));

        var page = await service.QueryAsync(
            new TenantSecurityAuditFilterRequest(),
            1);

        var item = Assert.Single(page.Items);
        Assert.Equal(101, item.TenantId);
        Assert.Equal("EmployeeStatusChanged", item.Action);
        Assert.Equal("Employee 1", item.ActorDisplayName);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.QueryAsync(new TenantSecurityAuditFilterRequest(), 2));
        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, exception.Code);
    }

    [Fact]
    public async Task CentralAuditQuery_RequiresActiveSystemAdminAndSupportsCentralFilters()
    {
        await using var centralDbContext = CreateCentralDbContext();
        centralDbContext.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "system-admin",
            PasswordHash = "not-exposed",
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        centralDbContext.SecurityAudits.AddRange(
            CentralAudit(101, "tenant-a", "TenantProvisioned"),
            CentralAudit(202, "tenant-b", "ManagerRoleChanged"));
        await centralDbContext.SaveChangesAsync();

        var service = new CentralSecurityAuditQueryService(centralDbContext);
        var page = await service.QueryAsync(
            new CentralSecurityAuditFilterRequest { TenantCode = "tenant-a" },
            1);

        var item = Assert.Single(page.Items);
        Assert.Equal("tenant-a", item.TenantCode);
        Assert.Equal("TenantProvisioned", item.Action);
        Assert.Equal("System Admin", item.ActorDisplayName);
        Assert.DoesNotContain(
            typeof(CentralSecurityAuditResponse).GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CentralSecurityAudit_IsAppendOnlyAtEfLevel()
    {
        await using var centralDbContext = CreateCentralDbContext();
        var audit = CentralAudit(101, "tenant-a", "SystemAdminLogin");
        centralDbContext.SecurityAudits.Add(audit);
        await centralDbContext.SaveChangesAsync();

        audit.Action = "Changed";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => centralDbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task TenantDeniedMiddleware_AuditsProtectedContractDenial()
    {
        await using var dbContext = CreateTenantDbContext();
        var currentTenant = CurrentTenant(101, "tenant-a");
        var writer = new TenantAuthorizationAuditWriter(
            dbContext,
            currentTenant,
            NullLogger<TenantAuthorizationAuditWriter>.Instance);
        var services = new ServiceCollection()
            .AddSingleton<ITenantAuthorizationAuditWriter>(writer)
            .BuildServiceProvider();
        var session = new TestSession();
        session.SetInt32("EmployeeId", 7);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Session = session
        };
        httpContext.Request.Path = "/api/contracts/42";
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new SessionAuthorizeAttribute()),
            "contract"));
        var middleware = new TenantDeniedAuthorizationAuditMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(httpContext);

        var audit = await dbContext.TblAuthorizationAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditActionTypes.AccessDenied, audit.Action);
        Assert.Equal(AuthorizationAuditResultTypes.Denied, audit.Result);
        Assert.Equal("Contract", audit.TargetType);
        Assert.Equal(7, audit.ActorEmployeeId);
        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, audit.FailureCode);
    }

    [Fact]
    public async Task SystemAdminAuthorize_InactiveSessionWritesCentralDeniedAudit()
    {
        await using var centralDbContext = CreateCentralDbContext();
        centralDbContext.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "inactive-admin",
            PasswordHash = "not-exposed",
            FullName = "Inactive System Admin",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });
        await centralDbContext.SaveChangesAsync();
        var writer = new CentralSecurityAuditWriter(
            centralDbContext,
            NullLogger<CentralSecurityAuditWriter>.Instance);
        var services = new ServiceCollection()
            .AddSingleton(centralDbContext)
            .AddSingleton<ICentralSecurityAuditWriter>(writer)
            .BuildServiceProvider();
        var session = new TestSession();
        session.SetInt32("SystemAdminId", 1);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Session = session
        };
        var filterContext = new AuthorizationFilterContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()),
            new List<IFilterMetadata>());

        await new SystemAdminAuthorizeAttribute().OnAuthorizationAsync(filterContext);

        var result = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        var audit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditActionTypes.CentralApiAccessDenied, audit.Action);
        Assert.Equal(AuthorizationAuditResultTypes.Denied, audit.Result);
        Assert.Equal(AuthorizationErrorCodes.AuthenticationRequired, audit.FailureCode);
        Assert.Null(session.GetInt32("SystemAdminId"));
    }

    [Fact]
    public async Task SystemAdminLogin_AuditsSuccessWithoutRecordingCredentials()
    {
        await using var centralDbContext = CreateCentralDbContext();
        var passwordHasher = new PasswordHasher<SystemAdmin>();
        var admin = new SystemAdmin
        {
            SystemAdminId = 1,
            Username = "system-admin",
            FullName = "System Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "CorrectPassword123!");
        centralDbContext.SystemAdmins.Add(admin);
        await centralDbContext.SaveChangesAsync();
        var controller = new SystemAuthController(
            centralDbContext,
            passwordHasher,
            new CentralSecurityAuditWriter(
                centralDbContext,
                NullLogger<CentralSecurityAuditWriter>.Instance));
        var httpContext = new DefaultHttpContext
        {
            Session = new TestSession()
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.Login(new SystemAdminLoginRequest
        {
            Username = "system-admin",
            Password = "CorrectPassword123!"
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var audit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal(AuthorizationAuditActionTypes.SystemAdminLogin, audit.Action);
        Assert.Equal(AuthorizationAuditResultTypes.Success, audit.Result);
        Assert.Equal(admin.SystemAdminId, audit.ActorSystemAdminId);
        Assert.DoesNotContain(
            "CorrectPassword123!",
            new[] { audit.TargetId, audit.FailureCode, audit.CorrelationId });
    }

    [Fact]
    public void SecurityAuditControllers_ExposeReadOnlyScopedEndpoints()
    {
        var tenantMethods = typeof(SecurityAuditsController).GetMethods();
        var centralMethods = typeof(CentralSecurityAuditsController).GetMethods();

        Assert.Contains(typeof(SecurityAuditsController)
            .GetCustomAttributes(typeof(SessionAuthorizeAttribute), true),
            attribute => ((SessionAuthorizeAttribute)attribute).GetType() == typeof(SessionAuthorizeAttribute));
        Assert.Contains(typeof(CentralSecurityAuditsController)
            .GetCustomAttributes(typeof(SystemAdminAuthorizeAttribute), true),
            attribute => ((SystemAdminAuthorizeAttribute)attribute).GetType() == typeof(SystemAdminAuthorizeAttribute));
        Assert.DoesNotContain(tenantMethods, method => method.Name.StartsWith("Delete", StringComparison.Ordinal)
            || method.Name.StartsWith("Put", StringComparison.Ordinal)
            || method.Name.StartsWith("Post", StringComparison.Ordinal));
        Assert.DoesNotContain(centralMethods, method => method.Name.StartsWith("Delete", StringComparison.Ordinal)
            || method.Name.StartsWith("Put", StringComparison.Ordinal)
            || method.Name.StartsWith("Post", StringComparison.Ordinal));
    }

    private static DbDtctechContext CreateTenantDbContext() => new(
        new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CentralDbContext CreateCentralDbContext() => new(
        new DbContextOptionsBuilder<CentralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CurrentTenant CurrentTenant(int tenantId, string tenantCode)
    {
        var currentTenant = new CurrentTenant();
        currentTenant.Set(new ResolvedTenant(
            tenantId,
            tenantCode,
            tenantCode,
            TenantDatabaseMode.Dedicated,
            "not-exposed"));
        return currentTenant;
    }

    private static TblEmployee NewEmployee(int employeeId, EmployeeType type) => new()
    {
        EmployeeId = employeeId,
        EmployeeAccount = $"employee-{employeeId}",
        EmployeeFullName = $"Employee {employeeId}",
        EmployeeType = (byte)type,
        Status = 1
    };

    private static TblAuthorizationAudit TenantAudit(
        int tenantId,
        int actorEmployeeId,
        string action) => new()
    {
        TenantId = tenantId,
        ActorEmployeeId = actorEmployeeId,
        ActorType = "Employee",
        Action = action,
        Result = AuthorizationAuditResultTypes.Success,
        TargetType = "Employee",
        TargetId = actorEmployeeId.ToString(),
        OccurredAt = DateTime.UtcNow,
        CorrelationId = "test"
    };

    private static CentralSecurityAudit CentralAudit(
        int tenantId,
        string tenantCode,
        string action) => new()
    {
        ActorSystemAdminId = 1,
        TenantId = tenantId,
        TenantCode = tenantCode,
        Action = action,
        Result = AuthorizationAuditResultTypes.Success,
        TargetType = "Tenant",
        TargetId = tenantCode,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = "test"
    };

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
