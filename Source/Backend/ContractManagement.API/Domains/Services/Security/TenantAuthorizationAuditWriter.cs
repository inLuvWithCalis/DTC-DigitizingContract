using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Security;

public sealed class TenantAuthorizationAuditWriter
    : ITenantAuthorizationAuditWriter
{
    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<TenantAuthorizationAuditWriter> _logger;

    public TenantAuthorizationAuditWriter(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant,
        ILogger<TenantAuthorizationAuditWriter> logger)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task TryWriteDeniedAsync(
        HttpContext httpContext,
        int? actorEmployeeId,
        string targetType,
        string? targetId,
        string failureCode,
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsResolved)
        {
            _logger.LogCritical(
                "Denied tenant API request could not be audited because no tenant was resolved. Target={TargetType}",
                targetType);
            return;
        }

        var record = AuthorizationAuditRecordFactory.CreateTenant(
            _currentTenant.Value!.TenantId,
            actorEmployeeId,
            "Employee",
            AuthorizationAuditActionTypes.AccessDenied,
            AuthorizationAuditResultTypes.Denied,
            targetType,
            targetId,
            null,
            null,
            null,
            null,
            failureCode,
            DateTime.UtcNow,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.TraceIdentifier);

        try
        {
            _dbContext.TblAuthorizationAudits.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _dbContext.Entry(record).State = EntityState.Detached;
            _logger.LogCritical(
                exception,
                "Denied tenant API request could not be written to authorization audit. Target={TargetType}, FailureCode={FailureCode}",
                targetType,
                failureCode);
        }
    }
}
