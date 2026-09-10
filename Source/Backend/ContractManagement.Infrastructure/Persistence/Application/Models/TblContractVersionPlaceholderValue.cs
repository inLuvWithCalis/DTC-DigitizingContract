namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractVersionPlaceholderValue
{
    public long ContractVersionPlaceholderValueId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int TemplateVersionId { get; set; }
    public string PlaceholderKey { get; set; } = string.Empty;
    public string SourceFieldKey { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string RenderedValue { get; set; } = string.Empty;
    public DateTime CapturedDate { get; set; }
}
