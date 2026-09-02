using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Security;

public sealed class CentralSecurityAuditWriter
    : ICentralSecurityAuditWriter
{
    private readonly CentralDbContext _centralDbContext;
    private readonly ILogger<CentralSecurityAuditWriter> _logger;

    public CentralSecurityAuditWriter(
        CentralDbContext centralDbContext,
        ILogger<CentralSecurityAuditWriter> logger)
    {
        _centralDbContext = centralDbContext;
        _logger = logger;
    }

    public async Task TryWriteAsync(
        HttpContext httpContext,
        CentralSecurityAuditWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var record = AuthorizationAuditRecordFactory.CreateCentral(
            request.ActorSystemAdminId,
            request.TenantId,
            request.TenantCode,
            request.Action,
            request.Result,
            request.TargetType,
            request.TargetId,
            request.PreviousEmployeeType,
            request.NewEmployeeType,
            request.PreviousStatus,
            request.NewStatus,
            request.FailureCode,
            DateTime.UtcNow,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.TraceIdentifier,
            request.ChangedFields);

        try
        {
            _centralDbContext.SecurityAudits.Add(record);
            await _centralDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _centralDbContext.Entry(record).State = EntityState.Detached;
            _logger.LogCritical(
                exception,
                "Central security audit could not be persisted. Action={Action}, Result={Result}, FailureCode={FailureCode}",
                request.Action,
                request.Result,
                request.FailureCode);
        }
    }
}
