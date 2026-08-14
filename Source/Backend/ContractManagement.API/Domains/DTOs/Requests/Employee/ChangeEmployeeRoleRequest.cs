using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Employee;

public sealed class ChangeEmployeeRoleRequest
{
    [Range(1, 6)]
    public byte EmployeeType { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
