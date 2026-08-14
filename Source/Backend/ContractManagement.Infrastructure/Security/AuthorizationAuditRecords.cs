using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central.Entities;

namespace ContractManagement.Infrastructure.Security;

public static class AuthorizationAuditActionTypes
{
    public const string AccessDenied = "AccessDenied";
    public const string CentralApiAccessDenied = "CentralApiAccessDenied";
    public const string SystemAdminLogin = "SystemAdminLogin";
    public const string EmployeeCreated = "EmployeeCreated";
    public const string EmployeeRoleChanged = "EmployeeRoleChanged";
    public const string EmployeeStatusChanged = "EmployeeStatusChanged";
    public const string EmployeePasswordReset = "EmployeePasswordReset";
    public const string TenantProvisioned = "TenantProvisioned";
    public const string ManagerRoleChanged = "ManagerRoleChanged";
}

public static class AuthorizationAuditResultTypes
{
    public const string Success = "Success";
    public const string Denied = "Denied";
    public const string Failed = "Failed";
}

public static class AuthorizationAuditRecordFactory
{
    public static TblAuthorizationAudit CreateTenant(
        int tenantId,
        int? actorEmployeeId,
        string actorType,
        string action,
        string result,
        string targetType,
        string? targetId,
        byte? previousEmployeeType,
        byte? newEmployeeType,
        byte? previousStatus,
        byte? newStatus,
        string? failureCode,
        DateTime occurredAt,
        string? ipAddress,
        string? userAgent,
        string correlationId)
    {
        return new TblAuthorizationAudit
        {
            TenantId = tenantId,
            ActorEmployeeId = actorEmployeeId,
            ActorType = actorType,
            Action = action,
            Result = result,
            TargetType = targetType,
            TargetId = targetId,
            PreviousEmployeeType = previousEmployeeType,
            NewEmployeeType = newEmployeeType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            FailureCode = failureCode,
            OccurredAt = occurredAt,
            IpAddress = Normalize(ipAddress, 45),
            UserAgent = Normalize(userAgent, 1024),
            CorrelationId = Normalize(correlationId, 100)
                ?? Guid.NewGuid().ToString("N")
        };
    }

    public static CentralSecurityAudit CreateCentral(
        int? actorSystemAdminId,
        int? tenantId,
        string? tenantCode,
        string action,
        string result,
        string? targetType,
        string? targetId,
        byte? previousEmployeeType,
        byte? newEmployeeType,
        byte? previousStatus,
        byte? newStatus,
        string? failureCode,
        DateTime occurredAt,
        string? ipAddress,
        string? userAgent,
        string correlationId)
    {
        return new CentralSecurityAudit
        {
            ActorSystemAdminId = actorSystemAdminId,
            TenantId = tenantId,
            TenantCode = Normalize(tenantCode, 50),
            Action = action,
            Result = result,
            TargetType = Normalize(targetType, 50),
            TargetId = Normalize(targetId, 100),
            PreviousEmployeeType = previousEmployeeType,
            NewEmployeeType = newEmployeeType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            FailureCode = Normalize(failureCode, 64),
            OccurredAt = occurredAt,
            IpAddress = Normalize(ipAddress, 45),
            UserAgent = Normalize(userAgent, 1024),
            CorrelationId = Normalize(correlationId, 100)
                ?? Guid.NewGuid().ToString("N")
        };
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
