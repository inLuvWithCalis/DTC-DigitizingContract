using ContractManagement.API.Common.Enums;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>
/// Tạo DOCX đã merge dữ liệu từ bytes của template đã validation.
/// Overload mặc định vẫn dùng dataset mẫu cho màn quản lý template; overload có renderData
/// được dùng để tạo tài liệu hợp đồng thật.
/// </summary>
public sealed class ContractTemplatePreviewRenderer : IContractTemplatePreviewRenderer
{
    public const string FormatVersion = "V4";

    private const string GeneratedContentFont = "Times New Roman";
    private const string GeneratedContentFontSize = "24";

    public byte[] RenderSample(byte[] sourceDocumentBytes, ContractLanguageMode languageMode,
        IReadOnlyList<SoftwareSupplyPlaceholderDefinition> definitions, IReadOnlyDictionary<string, string> customSamples)
    {
        var data = CreateSampleRenderData(languageMode);
        var values = new Dictionary<string, string>(data.ScalarValues, StringComparer.Ordinal);
        if (definitions.Any(x => x.IsSystem && x.Key == "CUSTOMER_REPRESENTATIVE_TITLE"))
            values["CUSTOMER_REPRESENTATIVE_TITLE"] = "Tổng giám đốc";
        foreach (var pair in customSamples) values[pair.Key] = pair.Value;
        return Render(sourceDocumentBytes, languageMode, data with { ScalarValues = values, Definitions = definitions });
    }

    public byte[] Render(byte[] sourceDocumentBytes, ContractLanguageMode languageMode)
        => Render(sourceDocumentBytes, languageMode, CreateSampleRenderData(languageMode));

    public byte[] Render(
        byte[] sourceDocumentBytes,
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData)
    {
        ArgumentNullException.ThrowIfNull(sourceDocumentBytes);
        ArgumentNullException.ThrowIfNull(renderData);
        if (sourceDocumentBytes.Length == 0)
        {
            throw new ContractTemplatePreviewException(
                "PreviewSourceUnavailable",
                "Không có bytes DOCX nguồn để tạo preview.");
        }

        using var output = new MemoryStream();
        output.Write(sourceDocumentBytes, 0, sourceDocumentBytes.Length);
        output.Position = 0;

        using (var document = WordprocessingDocument.Open(output, true))
        {
            var mainPart = document.MainDocumentPart
                ?? throw new ContractTemplatePreviewException(
                    "PreviewLayoutUnsupported",
                    "DOCX nguồn không có main document để preview.");
            if (mainPart.Document?.Body is null)
            {
                throw new ContractTemplatePreviewException(
                    "PreviewLayoutUnsupported",
                    "DOCX nguồn không có body để preview.");
            }

            var definitions = renderData.Definitions ?? ContractPlaceholderCatalog.SystemDefinitions;
            var dynamicParagraphs = LocateDynamicParagraphs(mainPart, definitions);
            ReplaceDynamicBlocks(dynamicParagraphs, languageMode, renderData);

            foreach (var root in GetTextRoots(mainPart))
            {
                ReplaceScalarTokens(root, renderData.ScalarValues);
            }

            EnsureNoCatalogTokensRemain(mainPart, definitions);
            mainPart.Document.Save();
        }

        return output.ToArray();
    }

    private static IReadOnlyDictionary<string, W.Paragraph> LocateDynamicParagraphs(
        MainDocumentPart mainPart, IReadOnlyList<SoftwareSupplyPlaceholderDefinition> definitions)
    {
        var dynamicKeys = definitions.Where(x => x.DataKind == TemplatePlaceholderDataKind.DynamicBlock).Select(x => x.Key).ToArray();
        var found = new Dictionary<string, List<W.Paragraph>>(
            StringComparer.Ordinal);
        foreach (var key in dynamicKeys)
        {
            found[key] = [];
        }

        foreach (var (root, isMainDocument) in GetTextRootsWithLocation(mainPart))
        {
            foreach (var paragraph in root.Descendants<W.Paragraph>().ToList())
            {
                var paragraphText = GetParagraphText(paragraph);
                foreach (var key in dynamicKeys)
                {
                    var token = Token(key);
                    if (!paragraphText.Contains(token, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!isMainDocument || !string.Equals(paragraphText, token,
                            StringComparison.Ordinal))
                    {
                        throw LayoutUnsupported(key);
                    }

                    found[key].Add(paragraph);
                }
            }
        }

        var result = new Dictionary<string, W.Paragraph>(StringComparer.Ordinal);
        foreach (var definition in definitions
                     .Where(item =>
                         item.DataKind == TemplatePlaceholderDataKind.DynamicBlock))
        {
            var matches = found[definition.Key];
            if (matches.Count == 0 && !definition.IsRequired)
            {
                continue;
            }

            if (matches.Count != 1)
            {
                throw LayoutUnsupported(definition.Key);
            }

            result[definition.Key] = matches[0];
        }

        return result;
    }

    private static void ReplaceDynamicBlocks(
        IReadOnlyDictionary<string, W.Paragraph> paragraphs,
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData)
    {
        foreach (var (key, paragraph) in paragraphs)
        {
            IEnumerable<OpenXmlElement> replacements = key switch
            {
                "CONTRACT_ITEM_TABLE" =>
                [
                    (OpenXmlElement)CreateItemTable(languageMode, renderData)
                ],
                "PAYMENT_SCHEDULE_TABLE" =>
                [
                    (OpenXmlElement)CreatePaymentTable(languageMode, renderData)
                ],
                "CONTRACT_LEGAL_BASES" => CreateLegalBasisElements(languageMode, renderData),
                "CONTRACT_TERMS" => CreateTermElements(languageMode, renderData),
                "SIGNATURE_PROVIDER" =>
                [
                    (OpenXmlElement)CreateSignatureBlock(
                        renderData.ProviderSignature,
                        renderData.Notice)
                ],
                "SIGNATURE_CUSTOMER" =>
                [
                    (OpenXmlElement)CreateSignatureBlock(
                        renderData.CustomerSignature,
                        renderData.Notice)
                ],
                _ => throw LayoutUnsupported(key)
            };

            ReplaceParagraph(paragraph, replacements);
        }
    }

    private static void ReplaceParagraph(
        W.Paragraph paragraph,
        IEnumerable<OpenXmlElement> replacements)
    {
        var parent = paragraph.Parent as OpenXmlCompositeElement
            ?? throw new ContractTemplatePreviewException(
                "PreviewLayoutUnsupported",
                "Placeholder động không có parent hợp lệ để thay thế.");
        var materialized = replacements.ToList();
        foreach (var replacement in materialized)
        {
            parent.InsertBefore(replacement, paragraph);
        }

        paragraph.Remove();
        // Word requires a table cell to end with a paragraph. A standalone
        // dynamic table in a cell gets a blank terminator without altering its
        // standardized table payload.
        if (parent is W.TableCell
            && !materialized.Any(element => element is W.Paragraph))
        {
            parent.Append(new W.Paragraph());
        }
    }

    private static IEnumerable<OpenXmlElement> CreateTermElements(
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData)
    {
        var elements = new List<OpenXmlElement>();
        if (!string.IsNullOrWhiteSpace(renderData.Notice))
        {
            elements.Add(CreateParagraph(renderData.Notice, bold: true));
        }

        foreach (var term in renderData.Terms)
        {
            var title = languageMode == ContractLanguageMode.Bilingual
                ? $"Điều {term.No}. {term.TitleVi} / Article {term.No}. {term.TitleEn}"
                : $"Điều {term.No}. {term.TitleVi}";
            elements.Add(CreateParagraph(title, bold: true));
            elements.AddRange(CreateTermContentElements(term.ContentVi));
            if (languageMode == ContractLanguageMode.Bilingual)
            {
                elements.AddRange(CreateTermContentElements(term.ContentEn));
            }
        }

        return elements;
    }

    private static IEnumerable<OpenXmlElement> CreateLegalBasisElements(
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData)
    {
        var elements = new List<OpenXmlElement>();
        foreach (var basis in renderData.LegalBases)
        {
            elements.AddRange(CreateTermContentElements(basis.ContentVi));
            if (languageMode == ContractLanguageMode.Bilingual
                && !string.IsNullOrWhiteSpace(basis.ContentEn))
            {
                elements.AddRange(CreateTermContentElements(basis.ContentEn));
            }
        }

        return elements;
    }

    private static IEnumerable<OpenXmlElement> CreateTermContentElements(string? value)
    {
        if (!ContractTermRichText.IsEncoded(value))
        {
            return (value ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(line => (OpenXmlElement)CreateParagraph(line))
                .ToList();
        }

        if (!ContractTermRichText.TryParse(value, out var document))
        {
            throw new ContractTemplatePreviewException(
                "ContractTermRichTextInvalid",
                "Nội dung điều khoản có định dạng rich text không hợp lệ.");
        }

        var elements = new List<OpenXmlElement>();
        foreach (var block in document.Blocks)
        {
            if (block.Type == "paragraph")
            {
                elements.Add(CreateRichParagraph(block.Runs));
                continue;
            }

            elements.Add(CreateRichTable(block.Rows));
        }

        if (elements.Count == 0)
        {
            elements.Add(CreateParagraph(string.Empty));
        }

        return elements;
    }

    private static W.Paragraph CreateRichParagraph(
        IEnumerable<ContractTermRichTextRun> runs)
    {
        var paragraph = new W.Paragraph();
        foreach (var run in runs)
        {
            paragraph.Append(CreateRichRun(run));
        }

        return paragraph;
    }

    private static W.Run CreateRichRun(ContractTermRichTextRun value)
    {
        var run = new W.Run(CreateGeneratedRunProperties(
            bold: value.Bold,
            fontSize: value.FontSize is null
                ? null
                : (value.FontSize.Value * 2).ToString(),
            italic: value.Italic,
            underline: value.Underline));
        var parts = value.Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
        for (var index = 0; index < parts.Length; index++)
        {
            if (index > 0) run.Append(new W.Break());
            if (parts[index].Length > 0) run.Append(Text(parts[index]));
        }

        return run;
    }

    private static W.Table CreateRichTable(
        IEnumerable<ContractTermRichTextRow> rows)
    {
        var table = new W.Table(CreateGeneratedTableProperties());
        foreach (var sourceRow in rows)
        {
            var row = new W.TableRow();
            foreach (var cell in sourceRow.Cells)
            {
                row.Append(new W.TableCell(
                    new W.TableCellProperties(
                        new W.TableCellWidth
                        {
                            Type = W.TableWidthUnitValues.Auto,
                            Width = "0"
                        }),
                    CreateRichParagraph(cell.Runs)));
            }

            table.Append(row);
        }

        return table;
    }

    private static W.Paragraph CreateSignatureBlock(
        ContractTemplateRenderSignature signature,
        string notice)
    {
        var paragraph = new W.Paragraph(
            CreateRun(signature.PartyTitle, bold: true),
            CreateBreakRun());
        if (!string.IsNullOrWhiteSpace(notice))
        {
            paragraph.Append(
                CreateRun(notice),
                CreateBreakRun());
        }

        paragraph.Append(
            CreateBreakRun(),
            CreateRun(signature.SignerName));
        return paragraph;
    }

    private static W.Table CreateItemTable(
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData)
    {
        var headers = languageMode == ContractLanguageMode.Bilingual
            ? new[] { "STT / No.", "Loại / Type", "Sản phẩm, dịch vụ / Description", "SL / Qty", "Đơn giá / Unit price", "CK / Disc.", "VAT", "Thành tiền / Total" }
            : new[] { "STT", "Loại", "Sản phẩm, dịch vụ", "SL", "Đơn giá", "CK", "VAT", "Thành tiền" };
        var rows = new List<IEnumerable<string>>
        {
            headers
        };

        rows.AddRange(renderData.Items.Select(item =>
            (IEnumerable<string>)
            [
                item.No.ToString(),
                item.Type,
                item.Description,
                FormatQuantity(item.Quantity),
                FormatMoney(item.UnitPrice, renderData.CurrencyCode),
                item.Discount,
                item.Vat,
                FormatMoney(item.TotalAmount, renderData.CurrencyCode)
            ]));

        var total = renderData.Items.Sum(item => item.TotalAmount);
        rows.Add(
        [
            string.Empty,
            string.Empty,
            renderData.Notice,
            string.Empty,
            string.Empty,
            string.Empty,
            "TỔNG CỘNG",
            FormatMoney(total, renderData.CurrencyCode)
        ]);

        return CreateTable(rows, headerRow: true);
    }

    private static W.Table CreatePaymentTable(
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData)
    {
        var headers = languageMode == ContractLanguageMode.Bilingual
            ? new[] { "Đợt / No.", "Nội dung / Description", "Tỷ lệ / Percent", "Số tiền / Amount", "Điều kiện / Due condition" }
            : new[] { "Đợt", "Nội dung", "Tỷ lệ", "Số tiền", "Điều kiện thanh toán" };
        var rows = new List<IEnumerable<string>>
        {
            headers
        };
        rows.AddRange(renderData.Payments.Select(payment =>
            (IEnumerable<string>)
            [
                payment.No.ToString(),
                payment.Description,
                payment.Percent,
                FormatMoney(payment.Amount, renderData.CurrencyCode),
                payment.DueCondition
            ]));
        if (renderData.Payments.Count == 0)
        {
            rows.Add([string.Empty, "Chưa có dữ liệu lịch thanh toán", string.Empty, string.Empty, string.Empty]);
        }
        return CreateTable(rows, headerRow: true);
    }

    private static W.Table CreateTable(
        IEnumerable<IEnumerable<string>> rows,
        bool headerRow)
    {
        var table = new W.Table(CreateGeneratedTableProperties());

        var rowIndex = 0;
        foreach (var values in rows)
        {
            var row = new W.TableRow();
            foreach (var value in values)
            {
                var paragraph = CreateParagraph(value, bold: headerRow && rowIndex == 0);
                row.Append(new W.TableCell(
                    new W.TableCellProperties(
                        new W.TableCellWidth
                        {
                            Type = W.TableWidthUnitValues.Auto,
                            Width = "0"
                        }),
                    paragraph));
            }

            table.Append(row);
            rowIndex++;
        }

        return table;
    }

    private static W.TableProperties CreateGeneratedTableProperties() =>
        new(
            new W.TableWidth { Type = W.TableWidthUnitValues.Pct, Width = "5000" },
            new W.TableBorders(
                new W.TopBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.LeftBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.BottomBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.RightBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4 }));

    private static void ReplaceScalarTokens(
        OpenXmlPartRootElement root,
        IReadOnlyDictionary<string, string> values)
    {
        foreach (var paragraph in root.Descendants<W.Paragraph>().ToList())
        {
            var textElements = paragraph.Descendants<W.Text>().ToList();
            if (textElements.Count == 0)
            {
                continue;
            }

            var before = string.Concat(textElements.Select(text => text.Text));
            // Match only original tokens once; values are literal text, never another substitution pass.
            var after = System.Text.RegularExpressions.Regex.Replace(before,
                @"\{\{([A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*)\}\}",
                match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);

            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                continue;
            }

            // Placeholder can be split between Word runs. Collapsing only the
            // affected paragraph guarantees replacement across those runs.
            textElements[0].Text = after;
            textElements[0].Space = SpaceProcessingModeValues.Preserve;
            foreach (var text in textElements.Skip(1))
            {
                text.Text = string.Empty;
            }
        }
    }

    private static void EnsureNoCatalogTokensRemain(MainDocumentPart mainPart, IReadOnlyList<SoftwareSupplyPlaceholderDefinition> definitions)
    {
        var text = string.Concat(GetTextRoots(mainPart)
            .SelectMany(root => root.Descendants<W.Text>())
            .Select(value => value.Text));
        var remaining = definitions
            .Select(definition => Token(definition.Key))
            .FirstOrDefault(token => text.Contains(token, StringComparison.Ordinal));
        if (remaining is not null || System.Text.RegularExpressions.Regex.IsMatch(text, @"\{\{[^{}]*\}\}"))
        {
            throw new ContractTemplatePreviewException(
                "PreviewRenderIncomplete",
                "Preview DOCX còn placeholder catalog chưa được thay thế.");
        }
    }

    private static IEnumerable<OpenXmlPartRootElement> GetTextRoots(
        MainDocumentPart mainPart) => GetTextRootsWithLocation(mainPart)
            .Select(item => item.Root);

    private static IEnumerable<(OpenXmlPartRootElement Root, bool IsMainDocument)>
        GetTextRootsWithLocation(MainDocumentPart mainPart)
    {
        if (mainPart.Document is not null)
        {
            yield return (mainPart.Document, true);
        }

        foreach (var header in mainPart.HeaderParts)
        {
            if (header.Header is not null)
            {
                yield return (header.Header, false);
            }
        }

        foreach (var footer in mainPart.FooterParts)
        {
            if (footer.Footer is not null)
            {
                yield return (footer.Footer, false);
            }
        }

        if (mainPart.FootnotesPart?.Footnotes is not null)
        {
            yield return (mainPart.FootnotesPart.Footnotes, false);
        }

        if (mainPart.EndnotesPart?.Endnotes is not null)
        {
            yield return (mainPart.EndnotesPart.Endnotes, false);
        }
    }

    private static string GetParagraphText(W.Paragraph paragraph) =>
        string.Concat(paragraph.Descendants<W.Text>().Select(text => text.Text));

    private static W.Paragraph CreateParagraph(string value, bool bold = false)
    {
        return new W.Paragraph(CreateRun(value, bold));
    }

    private static W.Run CreateRun(string value, bool bold = false) =>
        new(CreateGeneratedRunProperties(bold), Text(value));

    private static W.Run CreateBreakRun() =>
        new(CreateGeneratedRunProperties(), new W.Break());

    private static W.RunProperties CreateGeneratedRunProperties(
        bool bold = false,
        string? fontSize = null,
        bool italic = false,
        bool underline = false) =>
        new(
            new W.RunFonts
            {
                Ascii = GeneratedContentFont,
                HighAnsi = GeneratedContentFont,
                EastAsia = GeneratedContentFont,
                ComplexScript = GeneratedContentFont
            },
            new W.Bold { Val = bold },
            new W.Italic { Val = italic },
            new W.Underline
            {
                Val = underline ? W.UnderlineValues.Single : W.UnderlineValues.None
            },
            new W.FontSize { Val = fontSize ?? GeneratedContentFontSize },
            new W.FontSizeComplexScript { Val = fontSize ?? GeneratedContentFontSize });

    private static W.Text Text(string value) => new(value)
    {
        Space = SpaceProcessingModeValues.Preserve
    };

    private static string Token(string key) => $"{{{{{key}}}}}";

    private static ContractTemplateRenderData CreateSampleRenderData(
        ContractLanguageMode languageMode) => new(
        SoftwareSupplyPreviewDatasetV1.GetScalarValues(languageMode),
        SoftwareSupplyPreviewDatasetV1.Items.Select(item =>
            new ContractTemplateRenderItem(
                item.No,
                item.Type,
                item.Description,
                item.Quantity,
                item.UnitPrice,
                $"{item.DiscountPercent:0}%",
                $"{item.VatPercent:0}%",
                item.TotalAmount)).ToList(),
        SoftwareSupplyPreviewDatasetV1.Payments.Select(payment =>
            new ContractTemplateRenderPayment(
                payment.No,
                payment.Description,
                $"{payment.Percent:0}%",
                payment.Amount,
                payment.DueCondition)).ToList(),
        SoftwareSupplyPreviewDatasetV1.Terms.Select(term =>
            new ContractTemplateRenderTerm(
                term.No,
                term.TitleVi,
                term.TitleEn,
                term.ContentVi,
                term.ContentEn)).ToList(),
        new ContractTemplateRenderSignature(
            SoftwareSupplyPreviewDatasetV1.ProviderSignature.PartyTitle,
            SoftwareSupplyPreviewDatasetV1.ProviderSignature.SignerName),
        new ContractTemplateRenderSignature(
            SoftwareSupplyPreviewDatasetV1.CustomerSignature.PartyTitle,
            SoftwareSupplyPreviewDatasetV1.CustomerSignature.SignerName),
        SoftwareSupplyPreviewDatasetV1.LegalDisclaimer)
        {
            LegalBases = SoftwareSupplyPreviewDatasetV1.LegalBases.Select(basis =>
                new ContractTemplateRenderLegalBasis(
                    basis.No, basis.ContentVi, basis.ContentEn)).ToList()
        };

    private static ContractTemplatePreviewException LayoutUnsupported(string key) =>
        new(
            "PreviewLayoutUnsupported",
            $"Placeholder động {key} phải đứng một mình trong paragraph của main document.");

    private static string FormatMoney(decimal value, string currencyCode) =>
        $"{value:N0} {currencyCode}".Replace(',', '.');

    private static string FormatQuantity(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("0") : value.ToString("0.##");
}
