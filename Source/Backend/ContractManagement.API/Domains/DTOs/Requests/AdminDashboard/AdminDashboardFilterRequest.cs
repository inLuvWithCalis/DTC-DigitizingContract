namespace ContractManagement.API.Domains.DTOs.Requests.AdminDashboard;

public sealed class AdminDashboardFilterRequest
{
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }
}
