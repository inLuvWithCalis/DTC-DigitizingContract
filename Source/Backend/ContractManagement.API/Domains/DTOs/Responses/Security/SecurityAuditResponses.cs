namespace ContractManagement.API.Domains.DTOs.Responses.Security;

public sealed class TenantSecurityAuditResponse
{
    public long AuthorizationAuditId { get; set; }
    public int TenantId { get; set; }
    public int? ActorEmployeeId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? FailureCode { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public byte? PreviousEmployeeType { get; set; }
    public byte? NewEmployeeType { get; set; }
    public byte? PreviousStatus { get; set; }
    public byte? NewStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class CentralSecurityAuditResponse
{
    public long CentralSecurityAuditId { get; set; }
    public int? ActorSystemAdminId { get; set; }
    public string? ActorDisplayName { get; set; }
    public int? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? FailureCode { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public byte? PreviousEmployeeType { get; set; }
    public byte? NewEmployeeType { get; set; }
    public byte? PreviousStatus { get; set; }
    public byte? NewStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
