using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.DTOs.Requests.SystemAuth;

public sealed class SystemAdminLoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}