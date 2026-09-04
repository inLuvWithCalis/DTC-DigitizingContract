using ContractManagement.API.Common.Security;

namespace ContractManagement.API.Domains.DTOs.Responses.Authentication;

public sealed class EmployeePreferencesResponse
{
    public string DefaultPage { get; set; } = "/dashboard";
    public IReadOnlyList<EmployeeLandingPageOption> AvailableLandingPages { get; set; }
        = Array.Empty<EmployeeLandingPageOption>();
    public string RowVersion { get; set; } = string.Empty;
}
