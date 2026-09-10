using ContractManagement.API.Common.Enums;

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
    string Notice,
    string CurrencyCode = "VND")
{
    public IReadOnlyList<SoftwareSupplyPlaceholderDefinition>? Definitions { get; init; }

    public IReadOnlyList<ContractTemplateRenderLegalBasis> LegalBases { get; init; } = [];
}

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
    string ContentEn)
{
    public ContractTermKind Kind { get; init; } = ContractTermKind.General;
    public IReadOnlyList<ContractTemplateRenderPaymentMilestone> PaymentMilestones { get; init; } = [];
}

public sealed record ContractTemplateRenderPaymentMilestone(
    int No,
    string TitleVi,
    string? TitleEn,
    decimal PaymentPercent,
    decimal Amount,
    PaymentDueAnchor DueAnchor,
    int DueOffsetDays,
    PaymentDayCountMode DayCountMode,
    string? ConditionVi,
    string? ConditionEn,
    DateTime? DueDate = null);

public sealed record ContractTemplateRenderLegalBasis(
    int No,
    string ContentVi,
    string? ContentEn);

/// <summary>
/// Dữ liệu thật thuộc phạm vi template version được phép đưa vào preview tác giả.
/// Dữ liệu hợp đồng/khách hàng chưa tồn tại vẫn dùng sample dataset của renderer.
/// </summary>
public sealed record ContractTemplateAuthoringPreviewData
{
    public IReadOnlyList<ContractTemplateRenderLegalBasis> LegalBases { get; init; } = [];
    public IReadOnlyList<ContractTemplateRenderTerm> Terms { get; init; } = [];
}

public sealed record ContractTemplateRenderSignature(
    string PartyTitle,
    string SignerName);
