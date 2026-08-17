using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.DTOs.Responses.Employee;
using ContractManagement.Domains.Interfaces.Employee;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContractManagement.API.Domains.Services.Employee;

public sealed class SystemAdminManagerGovernanceService
    : ISystemAdminManagerGovernanceService
{
    private readonly CentralDbContext _centralDbContext;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SystemAdminManagerGovernanceService> _logger;

    public SystemAdminManagerGovernanceService(
        CentralDbContext centralDbContext,
        ITenantDbContextFactory tenantDbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SystemAdminManagerGovernanceService>? logger = null)
    {
        _centralDbContext = centralDbContext;
        _tenantDbContextFactory = tenantDbContextFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger ?? NullLogger<SystemAdminManagerGovernanceService>.Instance;
    }

    public async Task<ManagerGovernanceResponse> ChangeManagerRoleAsync(
        int systemAdminId,
        string tenantCode,
        int employeeId,
        ChangeEmployeeRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedTenantCode = NormalizeTenantCode(tenantCode);

        try
        {
            if (!await _centralDbContext.SystemAdmins.AnyAsync(
                    admin => admin.SystemAdminId == systemAdminId && admin.IsActive,
                    cancellationToken))
            {
                throw new RbacOperationException(
                    StatusCodes.Status401Unauthorized,
                    AuthorizationErrorCodes.AuthenticationRequired,
                    "System Admin session is no longer valid.");
            }

            if (!Enum.IsDefined(typeof(EmployeeType), request.EmployeeType))
            {
                throw new ArgumentException("Loại nhân viên không hợp lệ.");
            }

            var tenant = await _centralDbContext.Tenants
                .Include(candidate => candidate.TenantDatabase)
                .SingleOrDefaultAsync(
                    candidate => candidate.TenantCode == normalizedTenantCode
                        && candidate.Status == TenantStatus.Active,
                    cancellationToken);

            if (tenant is null)
            {
                throw new RbacOperationException(
                    StatusCodes.Status404NotFound,
                    AuthorizationErrorCodes.ResourceNotFound,
                    "Tenant was not found.");
            }

            await using var tenantDbContext = _tenantDbContextFactory
                .Create(tenant.TenantDatabase.ConnectionString);

            var outcome = await ExecuteTenantTransactionAsync(
                tenantDbContext,
                async () =>
                {
                    var employee = await tenantDbContext.TblEmployees
                        .FirstOrDefaultAsync(
                            candidate => candidate.EmployeeId == employeeId,
                            cancellationToken)
                        ?? throw new RbacOperationException(
                            StatusCodes.Status404NotFound,
                            AuthorizationErrorCodes.ResourceNotFound,
                            "Employee was not found.");

                    SetExpectedRowVersion(tenantDbContext, employee, request.RowVersion);

                    var isCurrentManager =
                        employee.EmployeeType == (byte)EmployeeType.Manager;
                    var isRequestedManager =
                        request.EmployeeType == (byte)EmployeeType.Manager;

                    if (!isCurrentManager && !isRequestedManager)
                    {
                        throw new RbacOperationException(
                            StatusCodes.Status403Forbidden,
                            AuthorizationErrorCodes.PermissionDenied,
                            "This endpoint only appoints or revokes the Manager role.");
                    }

                    if (employee.EmployeeType == request.EmployeeType)
                    {
                        throw new ArgumentException("Employee already has the requested role.");
                    }

                    if (isCurrentManager && employee.Status == 1 && !isRequestedManager)
                    {
                        var otherActiveManagerExists = await tenantDbContext.TblEmployees
                            .AnyAsync(
                                candidate => candidate.EmployeeId != employee.EmployeeId
                                    && candidate.EmployeeType == (byte)EmployeeType.Manager
                                    && candidate.Status == 1,
                                cancellationToken);

                        if (!otherActiveManagerExists)
                        {
                            throw new RbacOperationException(
                                StatusCodes.Status409Conflict,
                                AuthorizationErrorCodes.LastActiveManager,
                                "Tenant must retain at least one active Manager.");
                        }
                    }

                    var previousEmployeeType = employee.EmployeeType;
                    employee.EmployeeType = request.EmployeeType;
                    employee.DateModified = DateTime.UtcNow;

                    var httpContext = _httpContextAccessor.HttpContext;
                    tenantDbContext.TblAuthorizationAudits.Add(
                        AuthorizationAuditRecordFactory.CreateTenant(
                            tenant.TenantId,
                            null,
                            "SystemAdmin",
                            AuthorizationAuditActionTypes.ManagerRoleChanged,
                            AuthorizationAuditResultTypes.Success,
                            "Employee",
                            employee.EmployeeId.ToString(),
                            previousEmployeeType,
                            employee.EmployeeType,
                            employee.Status,
                            employee.Status,
                            null,
                            DateTime.UtcNow,
                            httpContext?.Connection.RemoteIpAddress?.ToString(),
                            httpContext?.Request.Headers.UserAgent.ToString(),
                            httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N")));

                    await tenantDbContext.SaveChangesAsync(cancellationToken);

                    return new TenantRoleChangeOutcome(
                        new ManagerGovernanceResponse
                        {
                            EmployeeId = employee.EmployeeId,
                            EmployeeType = employee.EmployeeType!.Value,
                            EmployeeTypeName = ((EmployeeType)employee.EmployeeType.Value).ToString(),
                            Status = employee.Status ?? 0,
                            RowVersion = employee.RowVersion is { Length: > 0 }
                                ? Convert.ToBase64String(employee.RowVersion)
                                : string.Empty
                        },
                        previousEmployeeType,
                        employee.Status);
                },
                cancellationToken);

            await WriteCentralAuditAsync(
                systemAdminId,
                tenant,
                AuthorizationAuditResultTypes.Success,
                null,
                outcome.Response.EmployeeId.ToString(),
                outcome.PreviousEmployeeType,
                outcome.Response.EmployeeType,
                outcome.PreviousStatus,
                outcome.Response.Status,
                cancellationToken);

            return outcome.Response;
        }
        catch (RbacOperationException exception)
        {
            await TryWriteCentralDeniedAuditAsync(
                systemAdminId,
                normalizedTenantCode,
                employeeId,
                exception.Code,
                cancellationToken);
            throw;
        }
    }

    private async Task WriteCentralAuditAsync(
        int systemAdminId,
        Tenant tenant,
        string result,
        string? failureCode,
        string? targetId,
        byte? previousEmployeeType,
        byte? newEmployeeType,
        byte? previousStatus,
        byte? newStatus,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        _centralDbContext.SecurityAudits.Add(
            AuthorizationAuditRecordFactory.CreateCentral(
                systemAdminId,
                tenant.TenantId,
                tenant.TenantCode,
                AuthorizationAuditActionTypes.ManagerRoleChanged,
                result,
                "Employee",
                targetId,
                previousEmployeeType,
                newEmployeeType,
                previousStatus,
                newStatus,
                failureCode,
                DateTime.UtcNow,
                httpContext?.Connection.RemoteIpAddress?.ToString(),
                httpContext?.Request.Headers.UserAgent.ToString(),
                httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N")));
        await _centralDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task TryWriteCentralDeniedAuditAsync(
        int systemAdminId,
        string tenantCode,
        int employeeId,
        string failureCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _centralDbContext.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.TenantCode == tenantCode,
                    cancellationToken);
            if (tenant is null)
            {
                return;
            }

            await WriteCentralAuditAsync(
                systemAdminId,
                tenant,
                AuthorizationAuditResultTypes.Denied,
                failureCode,
                employeeId.ToString(),
                null,
                null,
                null,
                null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "Denied System Admin manager-governance request could not be written to Central security audit.");
        }
    }

    private static async Task<T> ExecuteTenantTransactionAsync<T>(
        DbDtctechContext dbContext,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await operation();
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private static void SetExpectedRowVersion(
        DbDtctechContext dbContext,
        TblEmployee employee,
        string rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new ArgumentException("RowVersion không được để trống.");
        }

        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("RowVersion phải là Base64 hợp lệ.", exception);
        }

        if (expected.Length != 8
            || employee.RowVersion is not { Length: 8 }
            || !employee.RowVersion.AsSpan().SequenceEqual(expected))
        {
            throw new RbacOperationException(
                StatusCodes.Status409Conflict,
                AuthorizationErrorCodes.StaleRowVersion,
                "Employee has been updated by another request.");
        }

        dbContext.Entry(employee)
            .Property(candidate => candidate.RowVersion)
            .OriginalValue = expected;
    }

    private static string NormalizeTenantCode(string tenantCode)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            throw new RbacOperationException(
                StatusCodes.Status404NotFound,
                AuthorizationErrorCodes.ResourceNotFound,
                "Tenant was not found.");
        }

        return tenantCode.Trim().ToLowerInvariant();
    }

    private sealed record TenantRoleChangeOutcome(
        ManagerGovernanceResponse Response,
        byte? PreviousEmployeeType,
        byte? PreviousStatus);
}
