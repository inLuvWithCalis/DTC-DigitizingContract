namespace ContractManagement.Domains.Services.ContractTemplate;

public sealed class TemplatePdfRenderingOptions
{
    public const string SectionName = "TemplatePdfRendering";

    public string? ExecutablePath { get; set; }

    public int TimeoutSeconds { get; set; } = 60;

    public long MaxOutputBytes { get; set; } = 25 * 1024 * 1024;
}
