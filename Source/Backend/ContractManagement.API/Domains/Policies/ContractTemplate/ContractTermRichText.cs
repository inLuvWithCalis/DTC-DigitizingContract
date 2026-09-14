using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Schema định dạng có kiểm soát cho nội dung điều khoản. Không lưu HTML để tránh
/// phụ thuộc editor và để renderer DOCX có thể ánh xạ chính xác từng phần tử.
/// </summary>
public static class ContractTermRichText
{
    public const string Prefix = "contract-rich-text:v4:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsEncoded(string? value) =>
        value?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    public static bool TryParse(
        string? value,
        out ContractTermRichTextDocument document)
    {
        document = new ContractTermRichTextDocument();
        if (!IsEncoded(value))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ContractTermRichTextDocument>(
                value![Prefix.Length..],
                JsonOptions);
            if (parsed?.Blocks is null || parsed.Blocks.Count > 500)
            {
                return false;
            }

            foreach (var block in parsed.Blocks)
            {
                if (block.Type == "paragraph")
                {
                    if (!IsAlignmentValid(block.Alignment)
                        || !AreRunsValid(block.Runs)) return false;
                    continue;
                }

                if (block.Type != "table"
                    || block.Rows is null
                    || block.Rows.Count is 0 or > 50)
                {
                    return false;
                }

                foreach (var row in block.Rows)
                {
                    if (row.Cells is null
                        || row.Cells.Count > 20)
                    {
                        return false;
                    }

                    if (row.Cells.Any(cell => !IsCellValid(cell)))
                    {
                        return false;
                    }
                }

                if (!TryGetTableColumnCount(block.Rows, out var columnCount)
                    || !ContractTableLayoutPolicy.AreCanonicalWidths(
                        block.ColumnWidthsBps,
                        columnCount))
                {
                    return false;
                }
            }

            document = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool AreRunsValid(IReadOnlyCollection<ContractTermRichTextRun>? runs) =>
        runs is not null
        && runs.Count <= 2_000
        && runs.All(run =>
            run.Text is not null
            && (run.FontSize is null or >= 8 and <= 36));

    private static bool IsCellValid(ContractTermRichTextCell cell)
    {
        return cell.ExtensionData is null or { Count: 0 }
               && cell.Paragraphs is { Count: > 0 and <= 100 }
               && cell.Paragraphs.All(paragraph =>
                   IsAlignmentValid(paragraph.Alignment)
                   && AreRunsValid(paragraph.Runs));
    }

    private static bool TryGetTableColumnCount(
        IReadOnlyCollection<ContractTermRichTextRow> rows,
        out int columnCount)
    {
        var activeRowspans = new int[20];
        int? expectedWidth = null;
        columnCount = 0;

        foreach (var row in rows)
        {
            var occupied = activeRowspans.Select(remaining => remaining > 0).ToArray();
            for (var column = 0; column < activeRowspans.Length; column++)
            {
                if (activeRowspans[column] > 0) activeRowspans[column]--;
            }

            var cursor = 0;
            foreach (var cell in row.Cells)
            {
                var colspan = cell.Colspan ?? 1;
                var rowspan = cell.Rowspan ?? 1;
                if (colspan is < 1 or > 20
                    || rowspan is < 1 or > 50
                    || !IsVerticalAlignmentValid(cell.VerticalAlign))
                {
                    return false;
                }

                while (cursor < occupied.Length && occupied[cursor]) cursor++;
                if (cursor + colspan > occupied.Length)
                {
                    return false;
                }

                for (var column = cursor; column < cursor + colspan; column++)
                {
                    if (occupied[column]) return false;
                    occupied[column] = true;
                    if (rowspan > 1) activeRowspans[column] = rowspan - 1;
                }
                cursor += colspan;
            }

            var width = Array.FindLastIndex(occupied, value => value) + 1;
            if (width == 0 || expectedWidth is not null && width != expectedWidth)
            {
                return false;
            }
            expectedWidth ??= width;
        }

        if (!activeRowspans.All(remaining => remaining == 0)
            || expectedWidth is null)
        {
            return false;
        }

        columnCount = expectedWidth.Value;
        return true;
    }

    private static bool IsAlignmentValid(string? alignment) =>
        alignment is null or "left" or "center" or "right";

    private static bool IsVerticalAlignmentValid(string? alignment) =>
        alignment is null or "top" or "center" or "bottom";
}

public sealed class ContractTermRichTextDocument
{
    public List<ContractTermRichTextBlock> Blocks { get; set; } = [];
}

public sealed class ContractTermRichTextBlock
{
    public string Type { get; set; } = string.Empty;
    public List<ContractTermRichTextRun> Runs { get; set; } = [];
    public List<ContractTermRichTextRow> Rows { get; set; } = [];
    public List<int> ColumnWidthsBps { get; set; } = [];
    public string? Alignment { get; set; }
}

public sealed class ContractTermRichTextRow
{
    public List<ContractTermRichTextCell> Cells { get; set; } = [];
}

public sealed class ContractTermRichTextCell
{
    public List<ContractTermRichTextParagraph> Paragraphs { get; set; } = [];
    public int? Colspan { get; set; }
    public int? Rowspan { get; set; }
    public string? VerticalAlign { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ContractTermRichTextParagraph
{
    public List<ContractTermRichTextRun> Runs { get; set; } = [];
    public string? Alignment { get; set; }
}

public sealed class ContractTermRichTextRun
{
    public string Text { get; set; } = string.Empty;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public int? FontSize { get; set; }
}
