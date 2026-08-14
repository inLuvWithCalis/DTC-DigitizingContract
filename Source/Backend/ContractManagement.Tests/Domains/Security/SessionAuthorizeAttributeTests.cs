using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Options;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Middleware.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContractManagement.Tests.Domains.Security;

public sealed class SessionAuthorizeAttributeTests
{
    [Fact]
    public async Task TenantMiddleware_MissingEmployeeSession_ReturnsAuthenticationRequired()
    {
        var nextWasCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new MultiTenancyOptions()));
        var httpContext = new DefaultHttpContext
        {
            Session = new TestSession(),
            Response = { Body = new MemoryStream() }
        };
        httpContext.Request.Path = "/api/protected";
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new SessionAuthorizeAttribute()),
            "employee-protected"));

        await middleware.InvokeAsync(
            httpContext,
            new NeverCalledTenantResolver(),
            new CurrentTenant());

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains(AuthorizationErrorCodes.AuthenticationRequired, body);
    }

    [Fact]
    public async Task MissingSession_ReturnsAuthenticationRequired()
    {
        await using var context = CreateDbContext();
        var filterContext = CreateFilterContext(context, new TestSession());

        await new SessionAuthorizeAttribute().OnAuthorizationAsync(filterContext);

        var result = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal(
            AuthorizationErrorCodes.AuthenticationRequired,
            Assert.IsType<AuthorizationErrorResponse>(result.Value).Code);
    }

    [Fact]
    public async Task ActiveEmployeeWithPermission_IsLoadedFromTenantDatabase()
    {
        await using var context = CreateDbContext();
        context.TblEmployees.Add(NewEmployee(EmployeeType.Sale));
        await context.SaveChangesAsync();
        var session = AuthenticatedSession();
        var filterContext = CreateFilterContext(context, session);

        await new SessionAuthorizeAttribute(RbacPermissions.CustomerManage)
            .OnAuthorizationAsync(filterContext);

        Assert.Null(filterContext.Result);
        var employee = EmployeeAuthorizationContext.GetEmployee(
            filterContext.HttpContext);
        Assert.NotNull(employee);
        Assert.Equal(EmployeeType.Sale, employee.EmployeeType);
        Assert.Contains(RbacPermissions.CustomerManage, employee.Permissions);
    }

    [Fact]
    public async Task InactiveEmployee_ReturnsEmployeeInactiveAndClearsSession()
    {
        await using var context = CreateDbContext();
        context.TblEmployees.Add(NewEmployee(EmployeeType.Technical, status: 0));
        await context.SaveChangesAsync();
        var session = AuthenticatedSession();
        var filterContext = CreateFilterContext(context, session);

        await new SessionAuthorizeAttribute().OnAuthorizationAsync(filterContext);

        var result = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal(
            AuthorizationErrorCodes.EmployeeInactive,
            Assert.IsType<AuthorizationErrorResponse>(result.Value).Code);
        Assert.Null(session.GetInt32("EmployeeId"));
    }

    [Fact]
    public async Task InvalidEmployeeType_ReturnsPermissionDenied()
    {
        await using var context = CreateDbContext();
        context.TblEmployees.Add(NewEmployee(null));
        await context.SaveChangesAsync();
        var filterContext = CreateFilterContext(context, AuthenticatedSession());

        await new SessionAuthorizeAttribute().OnAuthorizationAsync(filterContext);

        var result = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(
            AuthorizationErrorCodes.PermissionDenied,
            Assert.IsType<AuthorizationErrorResponse>(result.Value).Code);
    }

    [Fact]
    public async Task RoleChange_TakesEffectOnNextRequestUsingSameSession()
    {
        await using var context = CreateDbContext();
        var employee = NewEmployee(EmployeeType.Sale);
        context.TblEmployees.Add(employee);
        await context.SaveChangesAsync();
        var session = AuthenticatedSession();

        var allowedRequest = CreateFilterContext(context, session);
        await new SessionAuthorizeAttribute(RbacPermissions.CustomerManage)
            .OnAuthorizationAsync(allowedRequest);
        Assert.Null(allowedRequest.Result);

        employee.EmployeeType = (byte)EmployeeType.Technical;
        await context.SaveChangesAsync();

        var deniedRequest = CreateFilterContext(context, session);
        await new SessionAuthorizeAttribute(RbacPermissions.CustomerManage)
            .OnAuthorizationAsync(deniedRequest);

        var result = Assert.IsType<ObjectResult>(deniedRequest.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(
            AuthorizationErrorCodes.PermissionDenied,
            Assert.IsType<AuthorizationErrorResponse>(result.Value).Code);
    }

    [Fact]
    public async Task StatusChange_TakesEffectOnNextRequestUsingSameSession()
    {
        await using var context = CreateDbContext();
        var employee = NewEmployee(EmployeeType.Sale);
        context.TblEmployees.Add(employee);
        await context.SaveChangesAsync();
        var session = AuthenticatedSession();

        var allowedRequest = CreateFilterContext(context, session);
        await new SessionAuthorizeAttribute(RbacPermissions.CustomerManage)
            .OnAuthorizationAsync(allowedRequest);
        Assert.Null(allowedRequest.Result);

        employee.Status = 0;
        await context.SaveChangesAsync();

        var deniedRequest = CreateFilterContext(context, session);
        await new SessionAuthorizeAttribute(RbacPermissions.CustomerManage)
            .OnAuthorizationAsync(deniedRequest);

        var result = Assert.IsType<ObjectResult>(deniedRequest.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal(
            AuthorizationErrorCodes.EmployeeInactive,
            Assert.IsType<AuthorizationErrorResponse>(result.Value).Code);
        Assert.Null(session.GetInt32("EmployeeId"));
    }

    private static DbDtctechContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }

    private static TblEmployee NewEmployee(
        EmployeeType? employeeType,
        byte status = 1)
    {
        return new TblEmployee
        {
            EmployeeId = 11,
            EmployeeAccount = "employee11",
            EmployeeFullName = "Employee 11",
            EmployeeType = employeeType.HasValue ? (byte)employeeType.Value : null,
            Status = status
        };
    }

    private static TestSession AuthenticatedSession()
    {
        var session = new TestSession();
        session.SetInt32("EmployeeId", 11);
        session.SetInt32("TenantId", 101);
        session.SetString("TenantCode", "tenant-a");
        return session;
    }

    private static AuthorizationFilterContext CreateFilterContext(
        DbDtctechContext dbContext,
        ISession session)
    {
        var currentTenant = new CurrentTenant();
        currentTenant.Set(new ResolvedTenant(
            101,
            "tenant-a",
            "Tenant A",
            TenantDatabaseMode.Dedicated,
            "unused"));

        var services = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<ICurrentTenant>(currentTenant)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Session = session
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());
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

    private sealed class NeverCalledTenantResolver : ITenantResolver
    {
        public Task<ResolvedTenant?> ResolveAsync(
            string tenantCode,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Tenant resolver must not run without a session.");
        }
    }
}
