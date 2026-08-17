namespace ContractManagement.API.Domains.DTOs.Responses.Employee;

/// <summary>
/// Minimal active-employee data for selecting a responsible employee.
/// </summary>
public sealed class EmployeeDirectoryResponse
{
    public int EmployeeId { get; set; }
    public string? EmployeeFullName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public byte EmployeeType { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public byte Status { get; set; }
}
