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
    public const string FormatVersion = "V6";

    private const string GeneratedContentFont = "Times New Roman";
    private const string GeneratedContentFontSize = "24";
    private const int RichTableWidthDxa = 9_000;

    public byte[] RenderSample(byte[] sourceDocumentBytes, ContractLanguageMode languageMode,
        IReadOnlyList<SoftwareSupplyPlaceholderDefinition> definitions,
        IReadOnlyDictionary<string, string> customSamples,
        ContractTemplateAuthoringPreviewData? authoringData = null)
    {
        var data = CreateSampleRenderData(languageMode);
        var values = new Dictionary<string, string>(data.ScalarValues, StringComparer.Ordinal);
        if (definitions.Any(x => x.IsSystem && x.Key == "CUSTOMER_REPRESENTATIVE_TITLE"))
            values["CUSTOMER_REPRESENTATIVE_TITLE"] = "Tổng giám đốc";
        foreach (var pair in customSamples) values[pair.Key] = pair.Value;
        var terms = authoringData is not null
            ? authoringData.Terms
            : data.Terms;
        var structuredPayments = terms.SelectMany(term => term.PaymentMilestones).ToList();
        var payments = structuredPayments.Count > 0
            ? structuredPayments.Select(item => new ContractTemplateRenderPayment(
                item.No,
                languageMode == ContractLanguageMode.Bilingual
                    && !string.IsNullOrWhiteSpace(item.TitleEn)
                        ? $"{item.TitleVi} / {item.TitleEn}"
                        : item.TitleVi,
                $"{item.PaymentPercent:0.####}%",
                item.Amount,
                languageMode == ContractLanguageMode.Bilingual
                    ? $"{BuildDueCondition(item, ContractLanguageMode.Vietnamese)} / "
                        + BuildDueCondition(item, ContractLanguageMode.Bilingual)
                    : BuildDueCondition(item, ContractLanguageMode.Vietnamese))).ToList()
            : data.Payments;
        return Render(sourceDocumentBytes, languageMode, data with
        {
            ScalarValues = values,
            Definitions = definitions,
            LegalBases = authoringData?.LegalBases ?? data.LegalBases,
            Terms = terms,
            Payments = payments
        });
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
            if (term.Kind == ContractTermKind.Payment)
            {
                foreach (var milestone in term.PaymentMilestones)
                    elements.Add(CreateParagraph(BuildMilestoneNarrative(
                        milestone, ContractLanguageMode.Vietnamese)));
            }
            if (languageMode == ContractLanguageMode.Bilingual)
            {
                elements.AddRange(CreateTermContentElements(term.ContentEn));
                if (term.Kind == ContractTermKind.Payment)
                {
                    foreach (var milestone in term.PaymentMilestones)
                        elements.Add(CreateParagraph(BuildMilestoneNarrative(
                            milestone, ContractLanguageMode.Bilingual)));
                }
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
                elements.Add(CreateRichParagraph(block.Runs, block.Alignment));
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
        IEnumerable<ContractTermRichTextRun> runs,
        string? alignment = null)
    {
        var paragraph = new W.Paragraph();
        if (ToJustification(alignment) is { } justification)
        {
            paragraph.ParagraphProperties = new W.ParagraphProperties(
                new W.Justification { Val = justification });
        }
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
        var layout = CreateRichTableLayout(rows.ToList());
        var tableProperties = CreateGeneratedTableProperties();
        tableProperties.Append(new W.TableLayout
        {
            Type = W.TableLayoutValues.Fixed
        });
        var table = new W.Table(tableProperties);
        var tableGrid = new W.TableGrid();
        foreach (var width in layout.ColumnWidths)
        {
            tableGrid.Append(new W.GridColumn { Width = width.ToString() });
        }
        table.Append(tableGrid);

        foreach (var sourceRow in layout.Rows)
        {
            var row = new W.TableRow();
            foreach (var layoutCell in sourceRow)
            {
                var properties = new W.TableCellProperties(
                    new W.TableCellWidth
                    {
                        Type = W.TableWidthUnitValues.Dxa,
                        Width = layout.ColumnWidths
                            .Skip(layoutCell.StartColumn)
                            .Take(layoutCell.Colspan)
                            .Sum()
                            .ToString()
                    });
                if (layoutCell.Colspan > 1)
                {
                    properties.Append(new W.GridSpan { Val = layoutCell.Colspan });
                }
                if (layoutCell.IsVerticalContinuation)
                {
                    properties.Append(new W.VerticalMerge
                    {
                        Val = W.MergedCellValues.Continue
                    });
                }
                else if ((layoutCell.Cell.Rowspan ?? 1) > 1)
                {
                    properties.Append(new W.VerticalMerge
                    {
                        Val = W.MergedCellValues.Restart
                    });
                }
                if (ToTableVerticalAlignment(layoutCell.Cell.VerticalAlign) is { } verticalAlignment)
                {
                    properties.Append(new W.TableCellVerticalAlignment
                    {
                        Val = verticalAlignment
                    });
                }

                var tableCell = new W.TableCell(properties);
                if (layoutCell.IsVerticalContinuation)
                {
                    tableCell.Append(new W.Paragraph());
                }
                else
                {
                    var cell = layoutCell.Cell;
                    var paragraphs = cell.Paragraphs.Count > 0
                        ? cell.Paragraphs.Select(paragraph =>
                        CreateRichParagraph(paragraph.Runs, paragraph.Alignment))
                        : [CreateRichParagraph(cell.Runs)];
                    tableCell.Append(paragraphs);
                }
                row.Append(tableCell);
            }

            table.Append(row);
        }

        return table;
    }

    private static RichTableLayout CreateRichTableLayout(
        IReadOnlyList<ContractTermRichTextRow> rows)
    {
        var columnCount = rows[0].Cells.Sum(cell => cell.Colspan ?? 1);
        var widthWeights = new int?[columnCount];
        var activeSpans = new List<ActiveRichTableSpan>();
        var layoutRows = new List<IReadOnlyList<RichTableLayoutCell>>(rows.Count);

        foreach (var sourceRow in rows)
        {
            var occupied = new bool[columnCount];
            var layoutCells = new List<RichTableLayoutCell>();
            var nextActiveSpans = new List<ActiveRichTableSpan>();
            foreach (var span in activeSpans)
            {
                for (var column = span.StartColumn;
                     column < span.StartColumn + span.Colspan;
                     column++)
                {
                    occupied[column] = true;
                }
                layoutCells.Add(new RichTableLayoutCell(
                    span.Cell,
                    span.StartColumn,
                    span.Colspan,
                    true));
                if (span.RemainingRows > 1)
                {
                    nextActiveSpans.Add(span with
                    {
                        RemainingRows = span.RemainingRows - 1
                    });
                }
            }

            var cursor = 0;
            foreach (var cell in sourceRow.Cells)
            {
                while (occupied[cursor]) cursor++;
                var colspan = cell.Colspan ?? 1;
                layoutCells.Add(new RichTableLayoutCell(cell, cursor, colspan, false));
                for (var column = cursor; column < cursor + colspan; column++)
                {
                    occupied[column] = true;
                    if (cell.Colwidth is { } colwidth && widthWeights[column] is null)
                    {
                        widthWeights[column] = colwidth[column - cursor];
                    }
                }
                if ((cell.Rowspan ?? 1) > 1)
                {
                    nextActiveSpans.Add(new ActiveRichTableSpan(
                        cell,
                        cursor,
                        colspan,
                        cell.Rowspan!.Value - 1));
                }
                cursor += colspan;
            }

            layoutRows.Add(layoutCells.OrderBy(cell => cell.StartColumn).ToList());
            activeSpans = nextActiveSpans;
        }

        var weights = widthWeights.Select(width => width ?? 100).ToArray();
        var totalWeight = weights.Sum();
        var columnWidths = weights
            .Select(weight => Math.Max(1, RichTableWidthDxa * weight / totalWeight))
            .ToArray();
        columnWidths[^1] += RichTableWidthDxa - columnWidths.Sum();

        return new RichTableLayout(layoutRows, columnWidths);
    }

    private static W.TableVerticalAlignmentValues? ToTableVerticalAlignment(
        string? alignment) => alignment switch
        {
            "top" => W.TableVerticalAlignmentValues.Top,
            "center" => W.TableVerticalAlignmentValues.Center,
            "bottom" => W.TableVerticalAlignmentValues.Bottom,
            _ => null
        };

    private sealed record RichTableLayout(
        IReadOnlyList<IReadOnlyList<RichTableLayoutCell>> Rows,
        IReadOnlyList<int> ColumnWidths);

    private sealed record RichTableLayoutCell(
        ContractTermRichTextCell Cell,
        int StartColumn,
        int Colspan,
        bool IsVerticalContinuation);

    private sealed record ActiveRichTableSpan(
        ContractTermRichTextCell Cell,
        int StartColumn,
        int Colspan,
        int RemainingRows);

    private static W.JustificationValues? ToJustification(string? alignment) =>
        alignment switch
        {
            "center" => W.JustificationValues.Center,
            "right" => W.JustificationValues.Right,
            "left" => W.JustificationValues.Left,
            _ => null
        };

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
            new W.FontSize { Val = fontSize ?? GeneratedContentFontSize },
            new W.FontSizeComplexScript { Val = fontSize ?? GeneratedContentFontSize },
            new W.Underline
            {
                Val = underline ? W.UnderlineValues.Single : W.UnderlineValues.None
            });

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

    private static string FormatMoney(decimal value, string currencyCode)
    {
        var decimals = string.Equals(currencyCode, "VND",
            StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        var culture = System.Globalization.CultureInfo.GetCultureInfo(
            decimals == 0 ? "vi-VN" : "en-US");
        return $"{value.ToString(decimals == 0 ? "N0" : "N2", culture)} {currencyCode}";
    }

    private static string FormatQuantity(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("0") : value.ToString("0.##");

    private static string BuildMilestoneNarrative(
        ContractTemplateRenderPaymentMilestone item,
        ContractLanguageMode languageMode)
    {
        if (languageMode == ContractLanguageMode.Bilingual)
        {
            var title = string.IsNullOrWhiteSpace(item.TitleEn) ? item.TitleVi : item.TitleEn;
            var days = item.DayCountMode == PaymentDayCountMode.BusinessDays
                ? "business days" : "days";
            var condition = BuildDueCondition(item, languageMode);
            return $"{title}: Pay {item.PaymentPercent:0.####}% of the contract value within {item.DueOffsetDays} {days} from {condition}.";
        }

        var dayLabel = item.DayCountMode == PaymentDayCountMode.BusinessDays
            ? "ngày làm việc" : "ngày";
        return $"{item.TitleVi}: Thanh toán {item.PaymentPercent:0.####}% giá trị hợp đồng trong vòng {item.DueOffsetDays} {dayLabel} kể từ {BuildDueCondition(item, languageMode)}.";
    }

    private static string BuildDueCondition(ContractTemplateRenderPaymentMilestone item,
        ContractLanguageMode languageMode)
    {
        var anchor = languageMode == ContractLanguageMode.Bilingual
            ? item.DueAnchor switch
            {
                PaymentDueAnchor.ContractSigned => "the contract signing date",
                PaymentDueAnchor.ContractEffectiveDate => "the effective date",
                PaymentDueAnchor.AcceptanceCompleted => "acceptance completion",
                PaymentDueAnchor.PreviousMilestonePaid => "full payment of the previous installment",
                _ => "the manually confirmed milestone"
            }
            : item.DueAnchor switch
            {
                PaymentDueAnchor.ContractSigned => "ngày ký hợp đồng",
                PaymentDueAnchor.ContractEffectiveDate => "ngày hợp đồng có hiệu lực",
                PaymentDueAnchor.AcceptanceCompleted => "ngày hoàn tất nghiệm thu",
                PaymentDueAnchor.PreviousMilestonePaid => "ngày thanh toán đủ đợt trước",
                _ => "mốc được xác nhận thủ công"
            };
        var condition = languageMode == ContractLanguageMode.Bilingual
            ? item.ConditionEn : item.ConditionVi;
        return string.IsNullOrWhiteSpace(condition) ? anchor : $"{anchor}; {condition.Trim()}";
    }
}
