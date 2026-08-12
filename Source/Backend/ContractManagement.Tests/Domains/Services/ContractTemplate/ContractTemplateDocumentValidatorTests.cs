using System.IO.Compression;
using System.Text;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.ContractTemplate;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.Tests.Domains.Services.ContractTemplate;

public sealed class ContractTemplateDocumentValidatorTests
{
    private readonly ContractTemplateDocumentValidator _validator = new();

    [Fact]
    public async Task ValidCatalogDocument_IsAccepted()
    {
        var result = await _validator.ValidateAsync(CreateFile(
            CreateDocument(RequiredTokens()),
            "template.docx"));

        Assert.True(result.IsTechnicallyAccepted,
            $"Technical failure: {result.FailureCode}");
        Assert.True(result.IsCatalogValid);
        Assert.Null(result.ValidationMessage);
        Assert.Equal(
            SoftwareSupplyPlaceholderCatalog.GetAll().Count(item => item.IsRequired),
            result.RecognizedPlaceholderKeys.Count);
        Assert.NotNull(result.DocumentBytes);
    }

    [Fact]
    public async Task MissingAndDuplicatedCatalogTokens_MarkDocumentInvalid()
    {
        var tokens = RequiredTokens()
            .Where(token => token != "{{CONTRACT_CODE}}")
            .Append("{{CONTRACT_NAME_EN}}")
            .Append("{{CONTRACT_NAME_EN}}");

        var result = await _validator.ValidateAsync(CreateFile(
            CreateDocument(tokens), "template.docx"));

        Assert.True(result.IsTechnicallyAccepted);
        Assert.False(result.IsCatalogValid);
        Assert.Contains("MissingRequiredPlaceholder:CONTRACT_CODE",
            result.ValidationMessage);
        Assert.Contains("MultiplicityViolation:CONTRACT_NAME_EN",
            result.ValidationMessage);
    }

    [Fact]
    public async Task UnknownAndMalformedTokens_MarkDocumentInvalidWithoutRawText()
    {
        var tokens = RequiredTokens()
            .Append("{{NOT_IN_CATALOG}}")
            .Append("{{contract_code}}")
            .Append("{{UNFINISHED");

        var result = await _validator.ValidateAsync(CreateFile(
            CreateDocument(tokens), "template.docx"));

        Assert.True(result.IsTechnicallyAccepted);
        Assert.False(result.IsCatalogValid);
        Assert.Contains("UnknownPlaceholder", result.ValidationMessage);
        Assert.Contains("InvalidPlaceholderSyntax", result.ValidationMessage);
        Assert.DoesNotContain("NOT_IN_CATALOG", result.ValidationMessage);
    }

    [Fact]
    public async Task SplitRunHeaderFooterAndTableTokens_AreAllRecognized()
    {
        var required = RequiredTokens().ToList();
        var result = await _validator.ValidateAsync(CreateFile(
            CreateDocument(
                required.Skip(3),
                headerTokens: ["{{CON", "TRACT_CODE}}"],
                footerTokens: [required[1]],
                tableTokens: [required[2]]),
            "template.docx"));

        Assert.True(result.IsTechnicallyAccepted,
            $"Technical failure: {result.FailureCode}");
        Assert.True(result.IsCatalogValid);
        Assert.Contains("CONTRACT_CODE", result.RecognizedPlaceholderKeys);
        Assert.Contains("CONTRACT_NAME", result.RecognizedPlaceholderKeys);
        Assert.Contains("CONTRACT_DATE", result.RecognizedPlaceholderKeys);
    }

    [Theory]
    [InlineData("template.doc")]
    [InlineData("template.docm")]
    [InlineData("template.dotx")]
    [InlineData("template.dotm")]
    [InlineData("template.pdf")]
    public async Task UnsupportedExtension_IsTechnicallyRejected(string fileName)
    {
        var result = await _validator.ValidateAsync(CreateFile(
            CreateDocument(RequiredTokens()), fileName));

        Assert.False(result.IsTechnicallyAccepted);
        Assert.Equal("DocxExtensionRequired", result.FailureCode);
    }

    [Fact]
    public async Task CorruptAndEncryptedLikePayloads_AreTechnicallyRejected()
    {
        var corrupt = await _validator.ValidateAsync(CreateFile(
            Encoding.UTF8.GetBytes("not a zip"), "template.docx"));
        var encryptedLike = await _validator.ValidateAsync(CreateFile(
            [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1],
            "template.docx"));

        Assert.False(corrupt.IsTechnicallyAccepted);
        Assert.Equal("NotZipPackage", corrupt.FailureCode);
        Assert.False(encryptedLike.IsTechnicallyAccepted);
        Assert.Equal("EncryptedOrPasswordProtected", encryptedLike.FailureCode);
    }

    [Fact]
    public async Task MacroTrackedChangesAndComments_AreTechnicallyRejected()
    {
        var macro = await _validator.ValidateAsync(CreateFile(
            AddMacroPart(CreateDocument(RequiredTokens())), "template.docx"));
        var tracked = await _validator.ValidateAsync(CreateFile(
            CreateDocument(RequiredTokens(), includeTrackedChange: true),
            "template.docx"));
        var comments = await _validator.ValidateAsync(CreateFile(
            CreateDocument(RequiredTokens(), includeComment: true),
            "template.docx"));

        Assert.Equal("MacroNotAllowed", macro.FailureCode);
        Assert.Equal("TrackedChangesNotAllowed", tracked.FailureCode);
        Assert.Equal("WordCommentsNotAllowed", comments.FailureCode);
    }

    [Fact]
    public async Task StreamOverTenMiB_IsTechnicallyRejected()
    {
        var result = await _validator.ValidateAsync(CreateFile(
            new byte[ContractTemplateDocumentValidator.MaxDocumentSizeBytes + 1],
            "template.docx"));

        Assert.False(result.IsTechnicallyAccepted);
        Assert.Equal("DocumentTooLarge", result.FailureCode);
    }

    private static IEnumerable<string> RequiredTokens() =>
        SoftwareSupplyPlaceholderCatalog.GetAll()
            .Where(item => item.IsRequired)
            .Select(item => $"{{{{{item.Key}}}}}");

    private static IFormFile CreateFile(byte[] bytes, string fileName) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.LongLength, "File", fileName);

    private static byte[] CreateDocument(
        IEnumerable<string> bodyTokens,
        IEnumerable<string>? headerTokens = null,
        IEnumerable<string>? footerTokens = null,
        IEnumerable<string>? tableTokens = null,
        bool includeTrackedChange = false,
        bool includeComment = false)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new W.Body();
            foreach (var token in bodyTokens)
            {
                body.Append(new W.Paragraph(new W.Run(new W.Text(token))));
            }

            if (tableTokens is not null)
            {
                var table = new W.Table(
                    new W.TableProperties(new W.TableWidth
                    {
                        Width = "0",
                        Type = W.TableWidthUnitValues.Auto
                    }),
                    new W.TableGrid(new W.GridColumn()),
                    new W.TableRow(new W.TableCell(
                        new W.TableCellProperties(),
                        new W.Paragraph(tableTokens.Select(token =>
                            new W.Run(new W.Text(token)))))));
                body.Append(table);
            }

            if (includeTrackedChange)
            {
                var inserted = new W.InsertedRun
                {
                    Id = "1",
                    Author = "editor",
                    Date = DateTime.UtcNow
                };
                inserted.Append(new W.Run(new W.Text("tracked")));
                body.Append(new W.Paragraph(inserted));
            }

            var section = new W.SectionProperties();
            if (headerTokens is not null)
            {
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                headerPart.Header = new W.Header(new W.Paragraph(
                    headerTokens.Select(token => new W.Run(new W.Text(token)))));
                headerPart.Header.Save();
                section.Append(new W.HeaderReference
                {
                    Type = W.HeaderFooterValues.Default,
                    Id = mainPart.GetIdOfPart(headerPart)
                });
            }

            if (footerTokens is not null)
            {
                var footerPart = mainPart.AddNewPart<FooterPart>();
                footerPart.Footer = new W.Footer(new W.Paragraph(
                    footerTokens.Select(token => new W.Run(new W.Text(token)))));
                footerPart.Footer.Save();
                section.Append(new W.FooterReference
                {
                    Type = W.HeaderFooterValues.Default,
                    Id = mainPart.GetIdOfPart(footerPart)
                });
            }

            body.Append(section);
            mainPart.Document = new W.Document(body);
            mainPart.Document.Save();

            if (includeComment)
            {
                var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
                commentsPart.Comments = new W.Comments(new W.Comment
                {
                    Id = "0",
                    Author = "editor",
                    Date = DateTime.UtcNow
                });
                commentsPart.Comments.Save();
            }
        }

        return stream.ToArray();
    }

    private static byte[] AddMacroPart(byte[] documentBytes)
    {
        using var stream = new MemoryStream();
        stream.Write(documentBytes);
        stream.Position = 0;
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Update,
                   leaveOpen: true))
        {
            var contentTypes = ReadEntry(archive, "[Content_Types].xml")
                .Replace("</Types>",
                    "<Override PartName=\"/word/vbaProject.bin\" " +
                    "ContentType=\"application/vnd.ms-office.vbaProject\" />" +
                    "</Types>",
                    StringComparison.Ordinal);
            ReplaceEntry(archive, "[Content_Types].xml", contentTypes);

            var relationshipsName = "word/_rels/document.xml.rels";
            var relationships = archive.GetEntry(relationshipsName) is null
                ? "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                  "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"></Relationships>"
                : ReadEntry(archive, relationshipsName);
            relationships = relationships.Replace("</Relationships>",
                "<Relationship Id=\"rIdSlice09Macro\" " +
                "Type=\"http://schemas.microsoft.com/office/2006/relationships/vbaProject\" " +
                "Target=\"vbaProject.bin\" /></Relationships>",
                StringComparison.Ordinal);
            ReplaceEntry(archive, relationshipsName, relationships);

            using var macroStream = archive.CreateEntry("word/vbaProject.bin")
                .Open();
            macroStream.WriteByte(0);
        }

        return stream.ToArray();
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());
        return reader.ReadToEnd();
    }

    private static void ReplaceEntry(ZipArchive archive, string name, string text)
    {
        archive.GetEntry(name)?.Delete();
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(text);
    }
}
