using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class AddContractAppendixRequest
{
    [Range(1, int.MaxValue)] public int TemplateAppendixId { get; set; }
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
}

public class CreateContractAppendixTermRequest
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

public sealed class UpdateContractAppendixTermRequest
    : CreateContractAppendixTermRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractAppendixRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class DeleteContractAppendixTermRequest
{
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Required] public string AppendixRowVersion { get; set; } = string.Empty;
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class ReorderContractAppendixTermsRequest
{
    [Required] public string VersionRowVersion { get; set; } = string.Empty;
    [Required] public string AppendixRowVersion { get; set; } = string.Empty;
    [Required] public List<ReorderContractAppendixTermItem> Terms { get; set; } = [];
}

public sealed class ReorderContractAppendixTermItem
{
    [Range(1, int.MaxValue)] public int AppendixTermId { get; set; }
    [Required] public string RowVersion { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
}
