namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Append-only tenant authorization audit.
/// </summary>
public sealed class TblAuthorizationAudit
{
    public long AuthorizationAuditId { get; set; }
    public int TenantId { get; set; }
    public int? ActorEmployeeId { get; set; }
    public string ActorType { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string Result { get; set; } = null!;
    public string? FailureCode { get; set; }
    public string TargetType { get; set; } = null!;
    public string? TargetId { get; set; }
    public byte? PreviousEmployeeType { get; set; }
    public byte? NewEmployeeType { get; set; }
    public byte? PreviousStatus { get; set; }
    public byte? NewStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = null!;
}
