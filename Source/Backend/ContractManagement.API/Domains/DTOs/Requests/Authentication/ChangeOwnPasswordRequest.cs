using System.ComponentModel.DataAnnotations;
using ContractManagement.API.Common.Security;

namespace ContractManagement.API.Domains.DTOs.Requests.Authentication;

public sealed class ChangeOwnPasswordRequest
{
    [Required]
    [MaxLength(AccountPasswordPolicy.MaximumLength)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(AccountPasswordPolicy.MinimumLength)]
    [MaxLength(AccountPasswordPolicy.MaximumLength)]
    public string NewPassword { get; set; } = string.Empty;
}
