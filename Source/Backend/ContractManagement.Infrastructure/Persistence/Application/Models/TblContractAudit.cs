namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Bản ghi business audit append-only thuộc một Contract trong tenant.
/// </summary>
public partial class TblContractAudit
{
    public int ContractAuditId { get; set; }

    public int TenantId { get; set; }

    public int ContractId { get; set; }

    public int? VersionId { get; set; }

    public string ActorType { get; set; } = null!;

    public int? ActorEmployeeId { get; set; }

    /// <summary>
    /// Non-secret logical session reference for customer-originated audit entries.
    /// </summary>
    public int? ActorCustomerAccessSessionId { get; set; }

    public string ActionType { get; set; } = null!;

    public string Result { get; set; } = null!;

    public byte? PreviousContractStatus { get; set; }

    public byte? NewContractStatus { get; set; }

    public int? PreviousResponsibleEmployeeId { get; set; }

    public int? NewResponsibleEmployeeId { get; set; }

    public string? Reason { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string CorrelationId { get; set; } = null!;
}
