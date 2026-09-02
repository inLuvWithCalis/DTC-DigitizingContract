using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Authentication;

public sealed class ProfileImageUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
