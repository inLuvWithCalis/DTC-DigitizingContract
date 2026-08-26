using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>
/// Reads only the DOCX structure and placeholder tokens required by Slice 09.
/// It deliberately does not interpret or retain prose from the document.
/// </summary>
public sealed class ContractTemplateDocumentValidator
    : IContractTemplateDocumentValidator
{
    public const long MaxDocumentSizeBytes = 10 * 1024 * 1024;

    private const long MaxUncompressedPackageBytes = 100 * 1024 * 1024;
    private const int MaxPackageEntries = 10_000;
    private const int MaxValidationMessageLength = 4_000;

    private static readonly Regex ValidTokenRegex = new(
        "^\\{\\{[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*\\}\\}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<ContractTemplateDocumentValidationResult> ValidateAsync(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var extension = GetSafeExtension(file?.FileName);
        if (file is null)
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                "DocumentMissing", extension, 0);
        }

        if (!string.Equals(extension, "docx", StringComparison.Ordinal))
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                "DocxExtensionRequired", extension, file.Length);
        }

        if (file.Length <= 0)
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                "DocumentEmpty", extension, 0);
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                "DocumentTooLarge", extension, file.Length);
        }

        var read = await ReadAtMostTenMiBAsync(file, cancellationToken);
        if (read.FailureCode is not null)
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                read.FailureCode, extension, read.FileSizeBytes);
        }

        var documentBytes = read.DocumentBytes!;
        if (!StartsWithZipSignature(documentBytes))
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                StartsWithCompoundFileSignature(documentBytes)
                    ? "EncryptedOrPasswordProtected"
                    : "NotZipPackage",
                extension,
                documentBytes.LongLength);
        }

        try
        {
            using var stream = new MemoryStream(documentBytes, writable: false);
            if (!IsSafeWordprocessingPackage(stream))
            {
                return ContractTemplateDocumentValidationResult.RejectTechnical(
                    "OoxmlStructureInvalid", extension, documentBytes.LongLength);
            }

            stream.Position = 0;
            if (ContainsMacroPart(stream))
            {
                return ContractTemplateDocumentValidationResult.RejectTechnical(
                    "MacroNotAllowed", extension, documentBytes.LongLength);
            }

            stream.Position = 0;
            using var document = WordprocessingDocument.Open(stream, false);
            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document is null)
            {
                return ContractTemplateDocumentValidationResult.RejectTechnical(
                    "OoxmlStructureInvalid", extension, documentBytes.LongLength);
            }

            if (document.Parts.Any(part => part.OpenXmlPart.ContentType.Contains(
                    "vbaProject", StringComparison.OrdinalIgnoreCase)))
            {
                return ContractTemplateDocumentValidationResult.RejectTechnical(
                    "MacroNotAllowed", extension, documentBytes.LongLength);
            }

            var roots = GetTextRoots(mainPart).ToList();

            // Do not use OpenXmlValidator as an upload gate. Real DOCX files
            // created by newer Microsoft 365 builds and third-party editors can
            // contain valid extension metadata that an older schema profile
            // reports as an error. Comments and tracked changes are also legal
            // WordprocessingML. They are ignored by placeholder extraction and
            // do not execute code. Invalid/missing placeholders still produce a
            // catalog-invalid version that cannot be published.

            return ValidatePlaceholders(roots, extension, documentBytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or OpenXmlPackageException
            or XmlException
            or ArgumentException)
        {
            return ContractTemplateDocumentValidationResult.RejectTechnical(
                "OoxmlStructureInvalid", extension, documentBytes.LongLength);
        }
    }

    private static ContractTemplateDocumentValidationResult ValidatePlaceholders(
        IReadOnlyCollection<OpenXmlElement> roots,
        string extension,
        byte[] documentBytes)
    {
        var catalog = SoftwareSupplyPlaceholderCatalog.GetAll();
        var catalogByKey = catalog.ToDictionary(
            item => item.Key,
            StringComparer.Ordinal);
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        var messages = new List<string>();
        var hasUnknownToken = false;

        foreach (var root in roots)
        {
            foreach (var paragraph in root.Descendants<Paragraph>())
            {
                // Text is joined per paragraph so {{TOKEN}} remains detectable
                // when Word has split it across formatting runs.
                var paragraphText = string.Concat(
                    paragraph.Descendants<Text>().Select(text => text.Text));
                foreach (var token in ParseTokens(paragraphText, messages))
                {
                    if (!catalogByKey.ContainsKey(token))
                    {
                        hasUnknownToken = true;
                        continue;
                    }

                    occurrences[token] = occurrences.GetValueOrDefault(token) + 1;
                }
            }
        }

        if (hasUnknownToken)
        {
            messages.Add("UnknownPlaceholder");
        }

        foreach (var definition in catalog)
        {
            var count = occurrences.GetValueOrDefault(definition.Key);
            if (definition.IsRequired && count == 0)
            {
                // Catalog keys are an allow-listed vocabulary, never document text.
                messages.Add($"MissingRequiredPlaceholder:{definition.Key}");
                continue;
            }

            var violatesMultiplicity = definition.Multiplicity switch
            {
                TemplatePlaceholderMultiplicity.ExactlyOne => count != 1,
                TemplatePlaceholderMultiplicity.ZeroOrOne => count > 1,
                _ => true
            };
            if (violatesMultiplicity)
            {
                messages.Add($"MultiplicityViolation:{definition.Key}");
            }
        }

        var safeMessages = messages
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var message = safeMessages.Count == 0
            ? null
            : LimitValidationMessage(string.Join(";", safeMessages));

        return new ContractTemplateDocumentValidationResult(
            IsTechnicallyAccepted: true,
            IsCatalogValid: safeMessages.Count == 0,
            RecognizedPlaceholderKeys: occurrences.Keys
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray(),
            FailureCode: safeMessages.Count == 0
                ? null
                : "CatalogValidationFailed",
            ValidationMessage: message,
            FileExtension: extension,
            FileSizeBytes: documentBytes.LongLength,
            DocumentBytes: documentBytes);
    }

    private static IEnumerable<string> ParseTokens(
        string paragraphText,
        ICollection<string> messages)
    {
        var tokens = new List<string>();
        var index = 0;
        while (index < paragraphText.Length)
        {
            if (StartsWith(paragraphText, index, "{{"))
            {
                var closingIndex = paragraphText.IndexOf("}}", index + 2,
                    StringComparison.Ordinal);
                if (closingIndex < 0)
                {
                    messages.Add("InvalidPlaceholderSyntax");
                    break;
                }

                var token = paragraphText[index..(closingIndex + 2)];
                if (!ValidTokenRegex.IsMatch(token))
                {
                    messages.Add("InvalidPlaceholderSyntax");
                }
                else
                {
                    tokens.Add(token[2..^2]);
                }

                index = closingIndex + 2;
                continue;
            }

            if (StartsWith(paragraphText, index, "}}"))
            {
                messages.Add("InvalidPlaceholderSyntax");
                index += 2;
                continue;
            }

            index++;
        }

        return tokens;
    }

    private static IEnumerable<OpenXmlElement> GetTextRoots(
        MainDocumentPart mainPart)
    {
        if (mainPart.Document is not null)
        {
            yield return mainPart.Document;
        }

        foreach (var headerPart in mainPart.HeaderParts)
        {
            if (headerPart.Header is not null)
            {
                yield return headerPart.Header;
            }
        }

        foreach (var footerPart in mainPart.FooterParts)
        {
            if (footerPart.Footer is not null)
            {
                yield return footerPart.Footer;
            }
        }

        if (mainPart.FootnotesPart?.Footnotes is not null)
        {
            yield return mainPart.FootnotesPart.Footnotes;
        }

        if (mainPart.EndnotesPart?.Endnotes is not null)
        {
            yield return mainPart.EndnotesPart.Endnotes;
        }
    }

    private static bool IsSafeWordprocessingPackage(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read,
            leaveOpen: true);
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaxPackageEntries)
        {
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalUncompressedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (!names.Add(entry.FullName) || entry.Length > MaxUncompressedPackageBytes)
            {
                return false;
            }

            totalUncompressedBytes += entry.Length;
            if (totalUncompressedBytes > MaxUncompressedPackageBytes)
            {
                return false;
            }
        }

        return names.Contains("[Content_Types].xml")
            && names.Contains("word/document.xml");
    }

    private static bool ContainsMacroPart(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read,
            leaveOpen: true);
        if (archive.Entries.Any(entry => entry.FullName.EndsWith(
                "vbaProject.bin", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var contentTypes = archive.GetEntry("[Content_Types].xml");
        if (contentTypes is null || contentTypes.Length > 1_000_000)
        {
            return false;
        }

        using var reader = new StreamReader(contentTypes.Open(), Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, bufferSize: 1024,
            leaveOpen: false);
        return reader.ReadToEnd().Contains("vbaProject",
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ReadResult> ReadAtMostTenMiBAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var source = file.OpenReadStream();
            await using var destination = new MemoryStream(
                (int)Math.Min(file.Length, MaxDocumentSizeBytes));
            var buffer = new byte[81_920];
            long total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total > MaxDocumentSizeBytes)
                {
                    return ReadResult.Rejected("DocumentTooLarge", total);
                }

                await destination.WriteAsync(buffer.AsMemory(0, read),
                    cancellationToken);
            }

            return total == 0
                ? ReadResult.Rejected("DocumentEmpty", 0)
                : ReadResult.Accepted(destination.ToArray(), total);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return ReadResult.Rejected("DocumentReadFailed", 0);
        }
    }

    private static bool StartsWithZipSignature(byte[] bytes) =>
        bytes.Length >= 4
        && bytes[0] == 0x50
        && bytes[1] == 0x4B
        && (bytes[2] == 0x03 || bytes[2] == 0x05 || bytes[2] == 0x07)
        && (bytes[3] == 0x04 || bytes[3] == 0x06 || bytes[3] == 0x08);

    private static bool StartsWithCompoundFileSignature(byte[] bytes) =>
        bytes.Length >= 8
        && bytes.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });

    private static bool StartsWith(string text, int index, string value) =>
        index + value.Length <= text.Length
        && text.AsSpan(index, value.Length).SequenceEqual(value);

    private static string GetSafeExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty)
            .Trim()
            .TrimStart('.')
            .ToLowerInvariant();
        return extension is "doc" or "docx" or "docm" or "dotx" or "dotm"
            ? extension
            : "other";
    }

    private static string LimitValidationMessage(string value) =>
        value.Length <= MaxValidationMessageLength
            ? value
            : value[..MaxValidationMessageLength];

    private sealed record ReadResult(
        byte[]? DocumentBytes,
        long FileSizeBytes,
        string? FailureCode)
    {
        public static ReadResult Accepted(byte[] bytes, long size) =>
            new(bytes, size, null);

        public static ReadResult Rejected(string failureCode, long size) =>
            new(null, size, failureCode);
    }
}
