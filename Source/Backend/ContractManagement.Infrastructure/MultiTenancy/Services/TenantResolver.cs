using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.Persistence.Central;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.MultiTenancy.Services;

/// <summary>
/// Tìm tenant và connection string trong Central Database.
/// </summary>
public sealed class TenantResolver : ITenantResolver
{
    private readonly CentralDbContext _centralDbContext;

    public TenantResolver(
        CentralDbContext centralDbContext)
    {
        _centralDbContext = centralDbContext;
    }

    public async Task<ResolvedTenant?> ResolveAsync(
        string tenantCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return null;
        }

        string normalizedTenantCode =
            tenantCode.Trim().ToLowerInvariant();

        /*
         * AsNoTracking:
         * Chỉ đọc dữ liệu, không cần EF theo dõi thay đổi.
         */
        return await _centralDbContext.Tenants
            .AsNoTracking()
            .Where(x =>
                x.TenantCode == normalizedTenantCode
                && x.Status == TenantStatus.Active)
            .Select(x => new ResolvedTenant(
                x.TenantId,
                x.TenantCode,
                x.TenantName,
                x.TenantDatabase.Mode,
                x.TenantDatabase.ConnectionString))
            .SingleOrDefaultAsync(cancellationToken);
    }
}