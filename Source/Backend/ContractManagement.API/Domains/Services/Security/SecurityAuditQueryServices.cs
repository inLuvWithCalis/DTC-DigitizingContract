using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Security;
using ContractManagement.API.Domains.DTOs.Responses.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Central;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Security;

public sealed class TenantSecurityAuditQueryService
    : ITenantSecurityAuditQueryService
{
    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public TenantSecurityAuditQueryService(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<TenantSecurityAuditResponse>> QueryAsync(
        TenantSecurityAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        Validate(filter);

        var canRead = await _dbContext.TblEmployees.AsNoTracking()
            .AnyAsync(employee => employee.EmployeeId == employeeId
                && employee.Status == 1
                && employee.EmployeeType == (byte)EmployeeType.Manager,
                cancellationToken);
        if (!canRead)
        {
            throw new RbacOperationException(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Only Manager may view tenant security audits.");
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var query = _dbContext.TblAuthorizationAudits
            .AsNoTracking()
            .Where(audit => audit.TenantId == tenantId);

        if (filter.ActorEmployeeId.HasValue)
        {
            query = query.Where(audit =>
                audit.ActorEmployeeId == filter.ActorEmployeeId.Value);
        }

        query = ApplyCommonFilter(query, filter);
        return await ToTenantPageAsync(query, filter, cancellationToken);
    }

    private static IQueryable<Infrastructure.Persistence.Application.Models.TblAuthorizationAudit>
        ApplyCommonFilter(
            IQueryable<Infrastructure.Persistence.Application.Models.TblAuthorizationAudit> query,
            SecurityAuditFilterRequest filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(audit => audit.Action == filter.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Result))
        {
            query = query.Where(audit => audit.Result == filter.Result.Trim());
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt <= filter.ToUtc.Value);
        }

        return query;
    }

    private async Task<PagedResult<TenantSecurityAuditResponse>> ToTenantPageAsync(
        IQueryable<Infrastructure.Persistence.Application.Models.TblAuthorizationAudit> query,
        SecurityAuditFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var auditRows = await query
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.AuthorizationAuditId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        var actorIds = auditRows
            .Where(audit => audit.ActorEmployeeId.HasValue)
            .Select(audit => audit.ActorEmployeeId!.Value)
            .Distinct()
            .ToList();
        var actorNames = await _dbContext.TblEmployees
            .AsNoTracking()
            .Where(employee => actorIds.Contains(employee.EmployeeId))
            .ToDictionaryAsync(
                employee => employee.EmployeeId,
                employee => employee.EmployeeFullName,
                cancellationToken);
        var audits = auditRows
            .Select(audit => new TenantSecurityAuditResponse
            {
                AuthorizationAuditId = audit.AuthorizationAuditId,
                TenantId = audit.TenantId,
                ActorEmployeeId = audit.ActorEmployeeId,
                ActorDisplayName = audit.ActorEmployeeId.HasValue
                    && actorNames.TryGetValue(audit.ActorEmployeeId.Value, out var name)
                        ? name
                        : null,
                ActorType = audit.ActorType,
                Action = audit.Action,
                Result = audit.Result,
                FailureCode = audit.FailureCode,
                TargetType = audit.TargetType,
                TargetId = audit.TargetId,
                PreviousEmployeeType = audit.PreviousEmployeeType,
                NewEmployeeType = audit.NewEmployeeType,
                PreviousStatus = audit.PreviousStatus,
                NewStatus = audit.NewStatus,
                OccurredAt = audit.OccurredAt,
                IpAddress = audit.IpAddress,
                UserAgent = audit.UserAgent,
                CorrelationId = audit.CorrelationId
            })
            .ToList();

        return new PagedResult<TenantSecurityAuditResponse>
        {
            Items = audits,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    internal static void Validate(SecurityAuditFilterRequest filter)
    {
        if (filter.Page <= 0 || filter.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("Page must be positive and PageSize must be from 1 to 100.");
        }

        if (filter.FromUtc.HasValue && filter.FromUtc.Value.Kind != DateTimeKind.Utc
            || filter.ToUtc.HasValue && filter.ToUtc.Value.Kind != DateTimeKind.Utc
            || filter.FromUtc > filter.ToUtc)
        {
            throw new ArgumentException("Audit time range must be valid UTC.");
        }

        if (filter.Action?.Length > 100 || filter.Result?.Length > 30)
        {
            throw new ArgumentException("Audit filter value is too long.");
        }
    }
}

public sealed class CentralSecurityAuditQueryService
    : ICentralSecurityAuditQueryService
{
    private readonly CentralDbContext _centralDbContext;

    public CentralSecurityAuditQueryService(CentralDbContext centralDbContext)
    {
        _centralDbContext = centralDbContext;
    }

    public async Task<PagedResult<CentralSecurityAuditResponse>> QueryAsync(
        CentralSecurityAuditFilterRequest filter,
        int systemAdminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        TenantSecurityAuditQueryService.Validate(filter);

        var isActive = await _centralDbContext.SystemAdmins.AsNoTracking()
            .AnyAsync(admin => admin.SystemAdminId == systemAdminId && admin.IsActive,
                cancellationToken);
        if (!isActive)
        {
            throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.AuthenticationRequired,
                "System Admin session is no longer valid.");
        }

        var query = _centralDbContext.SecurityAudits.AsNoTracking();
        if (filter.TenantId.HasValue)
        {
            query = query.Where(audit => audit.TenantId == filter.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TenantCode))
        {
            var tenantCode = filter.TenantCode.Trim().ToLowerInvariant();
            query = query.Where(audit => audit.TenantCode == tenantCode);
        }

        if (filter.ActorSystemAdminId.HasValue)
        {
            query = query.Where(audit =>
                audit.ActorSystemAdminId == filter.ActorSystemAdminId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(audit => audit.Action == filter.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Result))
        {
            query = query.Where(audit => audit.Result == filter.Result.Trim());
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(audit => audit.OccurredAt <= filter.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var auditRows = await query
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.CentralSecurityAuditId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        var actorIds = auditRows
            .Where(audit => audit.ActorSystemAdminId.HasValue)
            .Select(audit => audit.ActorSystemAdminId!.Value)
            .Distinct()
            .ToList();
        var actorNames = await _centralDbContext.SystemAdmins
            .AsNoTracking()
            .Where(admin => actorIds.Contains(admin.SystemAdminId))
            .ToDictionaryAsync(
                admin => admin.SystemAdminId,
                admin => admin.FullName,
                cancellationToken);
        var audits = auditRows
            .Select(audit => new CentralSecurityAuditResponse
            {
                CentralSecurityAuditId = audit.CentralSecurityAuditId,
                ActorSystemAdminId = audit.ActorSystemAdminId,
                ActorDisplayName = audit.ActorSystemAdminId.HasValue
                    && actorNames.TryGetValue(audit.ActorSystemAdminId.Value, out var name)
                        ? name
                        : null,
                TenantId = audit.TenantId,
                TenantCode = audit.TenantCode,
                Action = audit.Action,
                Result = audit.Result,
                FailureCode = audit.FailureCode,
                TargetType = audit.TargetType,
                TargetId = audit.TargetId,
                PreviousEmployeeType = audit.PreviousEmployeeType,
                NewEmployeeType = audit.NewEmployeeType,
                PreviousStatus = audit.PreviousStatus,
                NewStatus = audit.NewStatus,
                OccurredAt = audit.OccurredAt,
                IpAddress = audit.IpAddress,
                UserAgent = audit.UserAgent,
                CorrelationId = audit.CorrelationId
            })
            .ToList();

        return new PagedResult<CentralSecurityAuditResponse>
        {
            Items = audits,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}
