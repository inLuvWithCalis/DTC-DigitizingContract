namespace ContractManagement.API.Domains.DTOs.Responses.Employee;

public sealed class ManagerGovernanceResponse
{
    public int EmployeeId { get; set; }
    public byte EmployeeType { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public byte Status { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
