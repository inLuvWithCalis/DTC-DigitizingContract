using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Employee;

public sealed class SetEmployeeStatusRequest
{
    [Range(0, 1)]
    public byte Status { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
