namespace ContractManagement.API.Domains.DTOs.Requests.Employee;

public sealed class EmployeeDirectoryFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
}
