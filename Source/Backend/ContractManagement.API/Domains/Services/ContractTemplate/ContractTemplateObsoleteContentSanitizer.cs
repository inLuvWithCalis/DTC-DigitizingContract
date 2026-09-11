using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>
/// Removes document fragments that belonged to retired template features.
/// Sanitizing happens both when a DOCX is uploaded and when an older stored
/// template is rendered, so obsolete tokens never reach catalog validation or
/// generated artifacts.
/// </summary>
internal static class ContractTemplateObsoleteContentSanitizer
{
    internal const string RetiredPaymentScheduleKey =
        "PAYMENT_SCHEDULE_TABLE";
    private static readonly string RetiredPaymentScheduleToken =
        $"{{{{{RetiredPaymentScheduleKey}}}}}";
    private const string RetiredPaymentScheduleHeading =
        "LỊCH THANH TOÁN";

    public static byte[] Sanitize(byte[] sourceDocumentBytes)
    {
        ArgumentNullException.ThrowIfNull(sourceDocumentBytes);
        if (sourceDocumentBytes.Length == 0)
        {
            return sourceDocumentBytes;
        }

        using var output = new MemoryStream();
        output.Write(sourceDocumentBytes);
        output.Position = 0;
        var changed = false;

        using (var document = WordprocessingDocument.Open(output, true))
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document is null)
            {
                return sourceDocumentBytes;
            }

            var roots = GetTextRoots(mainPart).ToList();
            var affectedCells = new HashSet<W.TableCell>();
            foreach (var root in roots)
            {
                foreach (var paragraph in root.Descendants<W.Paragraph>().ToList())
                {
                    if (paragraph.Parent is null)
                    {
                        continue;
                    }

                    var text = string.Concat(
                        paragraph.Descendants<W.Text>().Select(item => item.Text));
                    if (!text.Contains(RetiredPaymentScheduleToken,
                            StringComparison.Ordinal)
                        && !string.Equals(text.Trim(),
                            RetiredPaymentScheduleHeading,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (paragraph.Parent is W.TableCell cell)
                    {
                        affectedCells.Add(cell);
                    }

                    paragraph.Remove();
                    changed = true;
                }
            }

            foreach (var cell in affectedCells.Where(cell =>
                         !cell.Descendants<W.Paragraph>().Any()))
            {
                cell.Append(new W.Paragraph());
            }

            if (changed)
            {
                foreach (var root in roots)
                {
                    root.Save();
                }
            }
        }

        return changed ? output.ToArray() : sourceDocumentBytes;
    }

    private static IEnumerable<OpenXmlPartRootElement> GetTextRoots(
        MainDocumentPart mainPart)
    {
        if (mainPart.Document is not null)
        {
            yield return mainPart.Document;
        }

        foreach (var part in mainPart.HeaderParts)
        {
            if (part.Header is not null)
            {
                yield return part.Header;
            }
        }

        foreach (var part in mainPart.FooterParts)
        {
            if (part.Footer is not null)
            {
                yield return part.Footer;
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
}
