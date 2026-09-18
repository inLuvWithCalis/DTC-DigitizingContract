using System.ComponentModel.DataAnnotations;
using ContractManagement.API.Common.Enums;
using ContractManagement.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;

public sealed class ContractTemplateFilterRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Keyword { get; set; }
}

public sealed class AvailableContractTemplateFilterRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Keyword { get; set; }

    public TemplateDocumentType? DocumentType { get; set; }

    public ContractLanguageMode? LanguageMode { get; set; }
}

public sealed class CreateContractTemplateRequest
{
    [Required]
    public string TemplateCode { get; set; } = string.Empty;

    [Required]
    public string TemplateName { get; set; } = string.Empty;

    public string? TemplateNameEn { get; set; }

    [Required]
    public ContractLanguageMode LanguageMode { get; set; }

    public string? Description { get; set; }

    public string? InitialChangeNote { get; set; }
}

public sealed class UpdateContractTemplateRequest
{
    [Required]
    public string TemplateName { get; set; } = string.Empty;

    public string? TemplateNameEn { get; set; }

    public string? Description { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class CopyContractTemplateVersionRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string? ChangeNote { get; set; }
}

/// <summary>
/// Multipart request. VersionRowVersion protects the Draft from a stale upload.
/// </summary>
public sealed class UploadContractTemplateDocumentRequest
{
    [Required]
    public IFormFile? File { get; set; }

    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

/// <summary>
/// Preview chỉ nhận RowVersion của Draft; dữ liệu render luôn là Dataset V1 cố định.
/// </summary>
public sealed class GenerateContractTemplatePreviewRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class UpdateContractTemplateItemTableLayoutRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;

    [Required]
    public List<int> ColumnWidthsBps { get; set; } = [];
}

public sealed class PublishContractTemplateVersionRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class RetireContractTemplateVersionRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class CreateContractTemplateTermRequest
{
    public ContractTermKind TermKind { get; set; } = ContractTermKind.General;

    [Required]
    public string TermCode { get; set; } = string.Empty;

    [Required]
    public string TermTitle { get; set; } = string.Empty;

    public string? TermTitleEn { get; set; }

    public string? TermContent { get; set; }

    public string? TermContentEn { get; set; }

    public bool IsNegotiable { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class UpdateContractTemplateTermRequest
{
    public ContractTermKind TermKind { get; set; } = ContractTermKind.General;

    [Required]
    public string TermCode { get; set; } = string.Empty;

    [Required]
    public string TermTitle { get; set; } = string.Empty;

    public string? TermTitleEn { get; set; }

    public string? TermContent { get; set; }

    public string? TermContentEn { get; set; }

    public bool IsNegotiable { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractTemplateTermRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class ReorderContractTemplateTermsRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;

    [Required]
    public List<ReorderContractTemplateTermItem> Terms { get; set; } = [];
}

public sealed class ReorderContractTemplateTermItem
{
    [Range(1, int.MaxValue)]
    public int TermId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

public class CreateContractTemplateAppendixRequest
{
    [Required, MaxLength(50)]
    public string AppendixCode { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string AppendixName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? AppendixNameEn { get; set; }
    [MaxLength(2000)]
    public string? AppendixDescription { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSelectedByDefault { get; set; }
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class UpdateContractTemplateAppendixRequest
    : CreateContractTemplateAppendixRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractTemplateAppendixRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class ReorderContractTemplateAppendicesRequest
{
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
    [Required] public List<ReorderContractTemplateAppendixItem> Appendices { get; set; } = [];
}

public sealed class ReorderContractTemplateAppendixItem
{
    [Range(1, int.MaxValue)] public int TemplateAppendixId { get; set; }
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
}

public class CreateContractTemplateAppendixTermRequest
{
    [Required, MaxLength(100)] public string TermCode { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string TermTitle { get; set; } = string.Empty;
    [MaxLength(1000)] public string? TermTitleEn { get; set; }
    public string? TermContent { get; set; }
    public string? TermContentEn { get; set; }
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
    [Required] public string AppendixRowVersion { get; set; } = string.Empty;
}

public sealed class UpdateContractTemplateAppendixTermRequest
    : CreateContractTemplateAppendixTermRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractTemplateAppendixTermRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Required] public string AppendixRowVersion { get; set; } = string.Empty;
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class ReorderContractTemplateAppendixTermsRequest
{
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
    [Required] public string AppendixRowVersion { get; set; } = string.Empty;
    [Required] public List<ReorderContractTemplateAppendixTermItem> Terms { get; set; } = [];
}

public sealed class ReorderContractTemplateAppendixTermItem
{
    [Range(1, int.MaxValue)] public int TemplateAppendixTermId { get; set; }
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
}

public class CreateContractTemplateLegalBasisRequest
{
    [Required]
    public string BasisCode { get; set; } = string.Empty;

    [Required]
    public string ContentVi { get; set; } = string.Empty;

    public string? ContentEn { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class UpdateContractTemplateLegalBasisRequest
    : CreateContractTemplateLegalBasisRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractTemplateLegalBasisRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class ReorderContractTemplateLegalBasesRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;

    [Required]
    public List<ReorderContractTemplateLegalBasisItem> LegalBases { get; set; } = [];
}

public sealed class ReorderContractTemplateLegalBasisItem
{
    [Range(1, int.MaxValue)]
    public int LegalBasisId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

public class SaveContractTemplatePaymentMilestoneRequest
{
    [Required, MaxLength(100)]
    public string MilestoneCode { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string TitleVi { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? TitleEn { get; set; }
    [Range(typeof(decimal), "0.0001", "100")]
    public decimal PaymentPercent { get; set; }
    public PaymentDueAnchor DueAnchor { get; set; }
    [Range(0, int.MaxValue)]
    public int DueOffsetDays { get; set; }
    public PaymentDayCountMode DayCountMode { get; set; }
    [MaxLength(2000)]
    public string? ConditionVi { get; set; }
    [MaxLength(2000)]
    public string? ConditionEn { get; set; }
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class CreateContractTemplatePaymentMilestoneRequest
    : SaveContractTemplatePaymentMilestoneRequest;

public sealed class UpdateContractTemplatePaymentMilestoneRequest
    : SaveContractTemplatePaymentMilestoneRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractTemplatePaymentMilestoneRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class ReorderContractTemplatePaymentMilestonesRequest
{
    [Required]
    public string VersionRowVersion { get; set; } = string.Empty;
    [Required]
    public List<ReorderContractTemplatePaymentMilestoneItem> Milestones { get; set; } = [];
}

public sealed class ReorderContractTemplatePaymentMilestoneItem
{
    [Range(1, int.MaxValue)]
    public int MilestoneId { get; set; }
    [Required]
    public string RowVersion { get; set; } = string.Empty;
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
