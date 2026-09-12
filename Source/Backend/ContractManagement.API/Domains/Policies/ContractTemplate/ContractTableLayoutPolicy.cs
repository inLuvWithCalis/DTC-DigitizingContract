using ContractManagement.Domains.Interfaces.ContractTemplate;

namespace ContractManagement.Domains.Policies.ContractTemplate;

public static class ContractTableLayoutPolicy
{
    public const int TotalBasisPoints = 10_000;
    public const int MinimumColumnBasisPoints = 250;

    public static readonly IReadOnlyList<string> ItemColumnKeys =
    [
        "No",
        "ItemType",
        "Description",
        "Quantity",
        "UnitPrice",
        "Discount",
        "Vat",
        "TotalAmount"
    ];

    public static readonly IReadOnlyList<int> DefaultItemColumnWidthsBps =
        [600, 900, 3_000, 650, 1_300, 900, 650, 2_000];

    public static bool AreCanonicalWidths(
        IReadOnlyList<int>? widths,
        int expectedColumnCount) =>
        widths is not null
        && expectedColumnCount > 0
        && widths.Count == expectedColumnCount
        && widths.All(width =>
            width is >= MinimumColumnBasisPoints and <= TotalBasisPoints)
        && widths.Sum() == TotalBasisPoints;

    public static IReadOnlyList<int> RequireCanonicalWidths(
        IReadOnlyList<int>? widths,
        int expectedColumnCount,
        string failureCode = "TableLayoutInvalid")
    {
        if (!AreCanonicalWidths(widths, expectedColumnCount))
        {
            throw new ContractTemplatePreviewException(
                failureCode,
                $"Độ rộng bảng phải có đúng {expectedColumnCount} cột, "
                + $"mỗi cột tối thiểu {MinimumColumnBasisPoints} bps "
                + $"và tổng đúng {TotalBasisPoints} bps.");
        }

        return widths!.ToArray();
    }

    public static IReadOnlyList<int> RequireItemColumnWidths(
        IEnumerable<(string ColumnKey, int DisplayOrder, int WidthBps)> rows)
    {
        var ordered = rows.OrderBy(row => row.DisplayOrder).ToArray();
        var isCanonical = ordered.Length == ItemColumnKeys.Count
            && ordered.Select(row => row.ColumnKey)
                .SequenceEqual(ItemColumnKeys, StringComparer.Ordinal)
            && ordered.Select(row => row.DisplayOrder)
                .SequenceEqual(Enumerable.Range(0, ItemColumnKeys.Count));
        if (!isCanonical)
        {
            throw new ContractTemplatePreviewException(
                "ItemTableLayoutInvalid",
                "Bố cục bảng sản phẩm/dịch vụ phải có đúng tám cột canonical.");
        }

        return RequireCanonicalWidths(
            ordered.Select(row => row.WidthBps).ToArray(),
            ItemColumnKeys.Count,
            "ItemTableLayoutInvalid");
    }

    public static IReadOnlyList<int> MapToDxa(
        IReadOnlyList<int> widthsBps,
        int availableWidthDxa)
    {
        RequireCanonicalWidths(widthsBps, widthsBps.Count);
        if (availableWidthDxa <= 0)
            throw new ArgumentOutOfRangeException(nameof(availableWidthDxa));

        var result = widthsBps
            .Select(width => availableWidthDxa * width / TotalBasisPoints)
            .ToArray();
        result[^1] += availableWidthDxa - result.Sum();
        return result;
    }
}
