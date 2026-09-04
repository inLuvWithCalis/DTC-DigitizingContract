using ContractManagement.API.Domains.DTOs.Requests.AdminDashboard;
using ContractManagement.API.Domains.DTOs.Responses.AdminDashboard;
using ContractManagement.API.Domains.Interfaces.Admin;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Admin;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private const int RecentLimit = 8;
    private readonly CentralDbContext _centralDbContext;

    public AdminDashboardService(CentralDbContext centralDbContext)
    {
        _centralDbContext = centralDbContext;
    }

    public async Task<AdminDashboardResponse> GetAsync(
        AdminDashboardFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var now = DateTime.UtcNow;
        var (fromUtc, toUtc) = NormalizeRange(filter, now);

        var tenantStatusCounts = await _centralDbContext.Tenants
            .AsNoTracking()
            .GroupBy(tenant => tenant.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Status, row => row.Count, cancellationToken);

        var recentTenants = await _centralDbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.CreatedAt >= fromUtc && tenant.CreatedAt <= toUtc)
            .OrderByDescending(tenant => tenant.CreatedAt)
            .ThenByDescending(tenant => tenant.TenantId)
            .Take(RecentLimit)
            .Select(tenant => new RecentTenantResponse(
                tenant.TenantId,
                tenant.TenantCode,
                tenant.TenantName,
                tenant.Status.ToString(),
                tenant.CreatedAt))
            .ToListAsync(cancellationToken);

        var provisioningFailures = await _centralDbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Status == TenantStatus.Failed
                && (tenant.UpdatedAt ?? tenant.CreatedAt) >= fromUtc
                && (tenant.UpdatedAt ?? tenant.CreatedAt) <= toUtc)
            .OrderByDescending(tenant => tenant.UpdatedAt ?? tenant.CreatedAt)
            .ThenByDescending(tenant => tenant.TenantId)
            .Take(RecentLimit)
            .Select(tenant => new TenantProvisioningFailureResponse(
                tenant.TenantId,
                tenant.TenantCode,
                tenant.TenantName,
                tenant.UpdatedAt ?? tenant.CreatedAt,
                "TenantProvisioningFailed"))
            .ToListAsync(cancellationToken);

        var auditWindow = _centralDbContext.SecurityAudits
            .AsNoTracking()
            .Where(audit => audit.OccurredAt >= fromUtc && audit.OccurredAt <= toUtc);
        var auditTrendRows = await auditWindow
            .Where(audit => audit.Result == AuthorizationAuditResultTypes.Denied
                || audit.Result == AuthorizationAuditResultTypes.Failed)
            .Select(audit => new
            {
                audit.OccurredAt,
                audit.Action,
                audit.Result
            })
            .ToListAsync(cancellationToken);

        var recentAuditRows = await auditWindow
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.CentralSecurityAuditId)
            .Take(RecentLimit)
            .ToListAsync(cancellationToken);
        var actorIds = recentAuditRows
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

        return new AdminDashboardResponse
        {
            GeneratedAt = now,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Summary =
            [
                new("total", tenantStatusCounts.Values.Sum()),
                new("active", Count(TenantStatus.Active)),
                new("pending", Count(TenantStatus.Pending)),
                new("provisioning", Count(TenantStatus.Provisioning)),
                new("suspended", Count(TenantStatus.Suspended)),
                new("failed", Count(TenantStatus.Failed))
            ],
            SecuritySeries = auditTrendRows
                .GroupBy(row => row.OccurredAt.ToString("yyyy-MM-dd"))
                .OrderBy(group => group.Key)
                .Select(group => new CentralSecurityTrendResponse(
                    group.Key,
                    group.Count(row => row.Result == AuthorizationAuditResultTypes.Denied),
                    group.Count(row => row.Action == AuthorizationAuditActionTypes.SystemAdminLogin
                        && row.Result != AuthorizationAuditResultTypes.Success)))
                .ToList(),
            RecentTenants = recentTenants,
            ProvisioningFailures = provisioningFailures,
            RecentAudits = recentAuditRows.Select(audit =>
                new RecentCentralAuditResponse(
                    audit.CentralSecurityAuditId,
                    audit.Action,
                    audit.Result,
                    ResolveActorName(audit.ActorSystemAdminId, actorNames),
                    audit.TenantCode,
                    audit.OccurredAt))
                .ToList()
        };

        int Count(TenantStatus status) =>
            tenantStatusCounts.GetValueOrDefault(status);
    }

    private static (DateTime FromUtc, DateTime ToUtc) NormalizeRange(
        AdminDashboardFilterRequest filter,
        DateTime now)
    {
        var fromUtc = filter.From?.UtcDateTime ?? now.Date.AddDays(-29);
        var toUtc = filter.To?.UtcDateTime ?? now;
        if (fromUtc > toUtc || toUtc - fromUtc > TimeSpan.FromDays(366 * 5))
        {
            throw new ArgumentException("Khoảng thời gian dashboard không hợp lệ.");
        }

        return (fromUtc, toUtc);
    }

    private static string? ResolveActorName(
        int? actorId,
        IReadOnlyDictionary<int, string> actorNames)
    {
        if (!actorId.HasValue)
        {
            return "Hệ thống";
        }

        return actorNames.TryGetValue(actorId.Value, out var name)
            ? name
            : $"System Admin #{actorId.Value}";
    }
}
