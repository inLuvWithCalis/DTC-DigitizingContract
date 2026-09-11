using System.Text.Json;

namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Schema định dạng có kiểm soát cho nội dung điều khoản. Không lưu HTML để tránh
/// phụ thuộc editor và để renderer DOCX có thể ánh xạ chính xác từng phần tử.
/// </summary>
public static class ContractTermRichText
{
    public const string Prefix = "contract-rich-text:v2:";
    public const string LegacyPrefix = "contract-rich-text:v1:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsEncoded(string? value) =>
        value?.StartsWith(Prefix, StringComparison.Ordinal) == true
        || value?.StartsWith(LegacyPrefix, StringComparison.Ordinal) == true;

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
            var prefixLength = value!.StartsWith(Prefix, StringComparison.Ordinal)
                ? Prefix.Length
                : LegacyPrefix.Length;
            var parsed = JsonSerializer.Deserialize<ContractTermRichTextDocument>(
                value[prefixLength..],
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
                    if (row.Cells is null || row.Cells.Count is 0 or > 20)
                    {
                        return false;
                    }

                    if (row.Cells.Any(cell => !IsCellValid(cell)))
                    {
                        return false;
                    }
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
        if (cell.Paragraphs is { Count: > 0 })
        {
            return cell.Paragraphs.Count <= 100
                   && cell.Paragraphs.All(paragraph =>
                       IsAlignmentValid(paragraph.Alignment)
                       && AreRunsValid(paragraph.Runs));
        }

        return AreRunsValid(cell.Runs);
    }

    private static bool IsAlignmentValid(string? alignment) =>
        alignment is null or "left" or "center" or "right";
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
    public string? Alignment { get; set; }
}

public sealed class ContractTermRichTextRow
{
    public List<ContractTermRichTextCell> Cells { get; set; } = [];
}

public sealed class ContractTermRichTextCell
{
    // Runs is retained for v1 documents. V2 stores paragraphs so alignment and
    // multiple paragraphs inside a cell survive round trips.
    public List<ContractTermRichTextRun> Runs { get; set; } = [];
    public List<ContractTermRichTextParagraph> Paragraphs { get; set; } = [];
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
