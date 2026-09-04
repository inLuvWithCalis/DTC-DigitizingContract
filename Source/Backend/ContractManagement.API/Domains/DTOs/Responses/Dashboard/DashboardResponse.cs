namespace ContractManagement.API.Domains.DTOs.Responses.Dashboard;

public sealed class DashboardResponse
{
    public string Scope { get; set; } = "Own";

    public DateTime GeneratedAt { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public IReadOnlyList<DashboardSummaryItemResponse> Summary { get; set; } = [];

    public IReadOnlyList<DashboardCurrencyAmountResponse> AmountByCurrency { get; set; } = [];

    public IReadOnlyList<DashboardVolumePointResponse> VolumeSeries { get; set; } = [];

    public IReadOnlyList<DashboardStatusPointResponse> StatusDistribution { get; set; } = [];

    public IReadOnlyList<ExpiringContractResponse> ExpiringContracts { get; set; } = [];

    public IReadOnlyList<RecentContractActivityResponse> RecentActivities { get; set; } = [];
}

public sealed record DashboardSummaryItemResponse(
    string Key,
    int Count,
    int? PreviousCount = null);

public sealed record DashboardCurrencyAmountResponse(
    string Currency,
    decimal Amount);

public sealed record DashboardVolumePointResponse(
    string Period,
    int Count);

public sealed record DashboardStatusPointResponse(
    string Status,
    int Count);

public sealed record ExpiringContractResponse(
    int ContractId,
    string ContractCode,
    string ContractName,
    DateTime ExpiresAt,
    string? ResponsibleEmployeeName);

public sealed record RecentContractActivityResponse(
    int AuditId,
    int ContractId,
    string ContractCode,
    string Action,
    string? ActorDisplayName,
    DateTime OccurredAt);
