namespace ContractManagement.API.Domains.DTOs.Requests.Security;

public class SecurityAuditFilterRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? Action { get; set; }

    public string? Result { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }
}

public sealed class TenantSecurityAuditFilterRequest : SecurityAuditFilterRequest
{
    public int? ActorEmployeeId { get; set; }
}

public sealed class CentralSecurityAuditFilterRequest : SecurityAuditFilterRequest
{
    public int? TenantId { get; set; }

    public string? TenantCode { get; set; }

    public int? ActorSystemAdminId { get; set; }
}
