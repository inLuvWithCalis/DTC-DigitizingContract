namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Snapshot dữ liệu đã được chuẩn bị để merge vào DOCX template.
/// Renderer không tự truy vấn database để bảo đảm dữ liệu của một lần render là nhất quán.
/// </summary>
public sealed record ContractTemplateRenderData(
    IReadOnlyDictionary<string, string> ScalarValues,
    IReadOnlyList<ContractTemplateRenderItem> Items,
    IReadOnlyList<ContractTemplateRenderPayment> Payments,
    IReadOnlyList<ContractTemplateRenderTerm> Terms,
    ContractTemplateRenderSignature ProviderSignature,
    ContractTemplateRenderSignature CustomerSignature,
    string Notice);

public sealed record ContractTemplateRenderItem(
    int No,
    string Type,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string Discount,
    string Vat,
    decimal TotalAmount);

public sealed record ContractTemplateRenderPayment(
    int No,
    string Description,
    string Percent,
    decimal Amount,
    string DueCondition);

public sealed record ContractTemplateRenderTerm(
    int No,
    string TitleVi,
    string TitleEn,
    string ContentVi,
    string ContentEn);

public sealed record ContractTemplateRenderSignature(
    string PartyTitle,
    string SignerName);
