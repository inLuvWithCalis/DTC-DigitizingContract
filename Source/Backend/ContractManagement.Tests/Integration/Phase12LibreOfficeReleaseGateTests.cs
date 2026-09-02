using ContractManagement.Domains.Services.ContractTemplate;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace ContractManagement.Tests.Integration;

public sealed class Phase12LibreOfficeReleaseGateTests
{
    private const string ExecutableVariable =
        "PHASE12_LIBREOFFICE_PATH";

    [Phase12LibreOfficeFact]
    [Trait("Category", "Phase12LibreOffice")]
    public async Task RealLibreOffice_ConvertsGeneratedDocxToValidPdf()
    {
        var executablePath = Environment.GetEnvironmentVariable(
            ExecutableVariable)!;
        Assert.True(File.Exists(executablePath));
        var renderer = new LibreOfficeContractTemplatePdfRenderer(
            Options.Create(new TemplatePdfRenderingOptions
            {
                ExecutablePath = executablePath,
                TimeoutSeconds = 60,
                MaxOutputBytes = 25 * 1024 * 1024
            }));

        var pdf = await renderer.ConvertPreviewToPdfAsync(CreateDocument());

        Assert.True(pdf.Length > 5);
        Assert.True(pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8));
    }

    private static byte[] CreateDocument()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   DocumentFormat.OpenXml.WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(
                new Body(
                    new Paragraph(
                        new Run(
                            new Text("Phase 12 LibreOffice release gate")))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private sealed class Phase12LibreOfficeFactAttribute : FactAttribute
    {
        public Phase12LibreOfficeFactAttribute()
        {
            var executable = Environment.GetEnvironmentVariable(
                ExecutableVariable);
            if (string.IsNullOrWhiteSpace(executable)
                || !File.Exists(executable))
            {
                Skip = $"Set {ExecutableVariable} to a LibreOffice executable.";
            }
        }
    }
}
