using System.Text.Json;

namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Schema định dạng có kiểm soát cho nội dung điều khoản. Không lưu HTML để tránh
/// phụ thuộc editor và để renderer DOCX có thể ánh xạ chính xác từng phần tử.
/// </summary>
public static class ContractTermRichText
{
    public const string Prefix = "contract-rich-text:v1:";

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
                    if (!AreRunsValid(block.Runs)) return false;
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

                    if (row.Cells.Any(cell => !AreRunsValid(cell.Runs)))
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
}

public sealed class ContractTermRichTextRow
{
    public List<ContractTermRichTextCell> Cells { get; set; } = [];
}

public sealed class ContractTermRichTextCell
{
    public List<ContractTermRichTextRun> Runs { get; set; } = [];
}

public sealed class ContractTermRichTextRun
{
    public string Text { get; set; } = string.Empty;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public int? FontSize { get; set; }
}
