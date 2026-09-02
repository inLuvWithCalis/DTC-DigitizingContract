namespace ContractManagement.Infrastructure.Persistence.Central.Entities;

/// <summary>
/// Append-only security audit for System Admin operations.
/// </summary>
public sealed class CentralSecurityAudit
{
    public long CentralSecurityAuditId { get; set; }
    public int? ActorSystemAdminId { get; set; }
    public int? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string Action { get; set; } = null!;
    public string Result { get; set; } = null!;
    public string? FailureCode { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? ChangedFields { get; set; }
    public byte? PreviousEmployeeType { get; set; }
    public byte? NewEmployeeType { get; set; }
    public byte? PreviousStatus { get; set; }
    public byte? NewStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = null!;
}
