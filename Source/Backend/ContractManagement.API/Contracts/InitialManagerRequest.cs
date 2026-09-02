using System.ComponentModel.DataAnnotations;
using ContractManagement.API.Common.Security;

namespace ContractManagement.Contracts.Tenants;

public sealed class InitialManagerRequest
{
    [MaxLength(30)]
    public string? EmployeeCode { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmployeeAccount { get; set; } = string.Empty;

    [Required]
    [MinLength(AccountPasswordPolicy.MinimumLength)]
    [MaxLength(AccountPasswordPolicy.MaximumLength)]
    public string EmployeePassword { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EmployeeFullName { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? EmployeeMobile { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? EmployeeEmail { get; set; }
}
