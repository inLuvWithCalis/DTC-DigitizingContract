using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Authentication;

public sealed class UpdateEmployeePreferencesRequest
{
    [Required]
    [MaxLength(200)]
    public string DefaultPage { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
