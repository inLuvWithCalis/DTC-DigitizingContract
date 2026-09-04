namespace ContractManagement.API.Domains.DTOs.Requests.Dashboard;

public sealed class DashboardFilterRequest
{
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public int ExpiryDays { get; set; } = 30;
}
