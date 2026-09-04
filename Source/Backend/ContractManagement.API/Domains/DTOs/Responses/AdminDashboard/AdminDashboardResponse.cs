namespace ContractManagement.API.Domains.DTOs.Responses.AdminDashboard;

public sealed class AdminDashboardResponse
{
    public DateTime GeneratedAt { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public IReadOnlyList<AdminDashboardSummaryResponse> Summary { get; set; } = [];

    public IReadOnlyList<CentralSecurityTrendResponse> SecuritySeries { get; set; } = [];

    public IReadOnlyList<RecentTenantResponse> RecentTenants { get; set; } = [];

    public IReadOnlyList<TenantProvisioningFailureResponse> ProvisioningFailures { get; set; } = [];

    public IReadOnlyList<RecentCentralAuditResponse> RecentAudits { get; set; } = [];
}

public sealed record AdminDashboardSummaryResponse(string Key, int Count);

public sealed record CentralSecurityTrendResponse(
    string Period,
    int DeniedCount,
    int LoginFailureCount);

public sealed record RecentTenantResponse(
    int TenantId,
    string TenantCode,
    string TenantName,
    string Status,
    DateTime CreatedAt);

public sealed record TenantProvisioningFailureResponse(
    int TenantId,
    string TenantCode,
    string TenantName,
    DateTime OccurredAt,
    string FailureCode);

public sealed record RecentCentralAuditResponse(
    long AuditId,
    string Action,
    string Result,
    string? ActorDisplayName,
    string? TenantCode,
    DateTime OccurredAt);
