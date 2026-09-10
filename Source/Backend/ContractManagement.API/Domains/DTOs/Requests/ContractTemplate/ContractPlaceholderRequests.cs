using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;

public sealed class SaveContractPlaceholderRequest
{
    [Required, MaxLength(104)] public string PlaceholderKey { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string FieldLabel { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string SourceFieldKey { get; set; } = string.Empty;
    [MaxLength(2000)] public string? DefaultValue { get; set; }
    [MaxLength(100)] public string? FormatString { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class PlaceholderRowVersionRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
}
