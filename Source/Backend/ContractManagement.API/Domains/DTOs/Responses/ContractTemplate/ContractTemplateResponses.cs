using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;

public sealed class SoftwareSupplyPlaceholderCatalogResponse
{
    public string CatalogVersion { get; set; } = SoftwareSupplyPlaceholderCatalog.Version;

    public IReadOnlyList<SoftwareSupplyPlaceholderDefinition> Items { get; set; } = [];
}

/// <summary>
/// Minimal current published template version that may be selected for a new contract.
/// </summary>
public sealed class AvailableContractTemplateVersionResponse
{
    public int TemplateId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public string? TemplateNameEn { get; set; }

    public TemplateDocumentType DocumentType { get; set; }

    public ContractLanguageMode LanguageMode { get; set; }

    public int TemplateVersionId { get; set; }

    public int VersionNo { get; set; }
}

public class ContractTemplateResponse
{
    public int TemplateId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public string? TemplateNameEn { get; set; }

    public TemplateDocumentType DocumentType { get; set; }

    public ContractLanguageMode LanguageMode { get; set; }

    public string? Description { get; set; }

    public int? CurrentPublishedVersionId { get; set; }

    public bool IsActive { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractTemplateDetailResponse : ContractTemplateResponse
{
    public List<ContractTemplateVersionSummaryResponse> Versions { get; set; } = [];
}

public sealed class ContractTemplateVersionSummaryResponse
{
    public int TemplateVersionId { get; set; }

    public int VersionNo { get; set; }

    public string? ChangeNote { get; set; }

    public TemplateVersionStatus Status { get; set; }

    public TemplateValidationStatus ValidationStatus { get; set; }

    public int? DocumentFileId { get; set; }

    public int? PublishedPreviewPdfFileId { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public sealed class ContractTemplateVersionDetailResponse
{
    public int TemplateVersionId { get; set; }

    public int TemplateId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public int VersionNo { get; set; }

    public string? ChangeNote { get; set; }

    public TemplateVersionStatus Status { get; set; }

    public TemplateValidationStatus ValidationStatus { get; set; }

    public string? ValidationMessage { get; set; }

    public int? DocumentFileId { get; set; }

    public string? DocumentHash { get; set; }

    public int? PreviewFileId { get; set; }

    public int? PublishedPreviewPdfFileId { get; set; }

    public string? PreviewSourceHash { get; set; }

    public DateTime? PreviewedAt { get; set; }

    public int? PreviewedByEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public List<ContractTemplateTermResponse> Terms { get; set; } = [];
}

public sealed class ContractTemplatePreviewResponse
{
    public int TemplateVersionId { get; set; }

    public int PreviewFileId { get; set; }

    public DateTime PreviewedAt { get; set; }

    public int PreviewedByEmployeeId { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsReused { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractTemplateTermResponse
{
    public int TemplateTermId { get; set; }

    public int TemplateVersionId { get; set; }

    public string TermCode { get; set; } = string.Empty;

    public string TermTitle { get; set; } = string.Empty;

    public string? TermTitleEn { get; set; }

    public string? TermContent { get; set; }

    public string? TermContentEn { get; set; }

    public bool IsNegotiable { get; set; }

    public int DisplayOrder { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractTemplatePageResponse : PagedResult<ContractTemplateResponse>
{
}
