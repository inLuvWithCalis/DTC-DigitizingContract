using System.Text.Json;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Tests;

internal static class ContractRichTextTestData
{
    internal static string RichText(
        string text,
        string? alignment = null,
        bool bold = false) =>
        ContractTermRichText.Prefix + JsonSerializer.Serialize(
            new ContractTermRichTextDocument
            {
                Blocks =
                [
                    new ContractTermRichTextBlock
                    {
                        Type = "paragraph",
                        Alignment = alignment,
                        Runs =
                        [
                            new ContractTermRichTextRun
                            {
                                Text = text,
                                Bold = bold
                            }
                        ]
                    }
                ]
            });
}
