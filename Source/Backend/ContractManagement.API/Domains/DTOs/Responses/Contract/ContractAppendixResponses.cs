namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractAppendixResponse
{
    public int AppendixId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int SourceTemplateAppendixId { get; set; }
    public string AppendixCode { get; set; } = string.Empty;
    public string AppendixName { get; set; } = string.Empty;
    public string? AppendixNameEn { get; set; }
    public string? AppendixDescription { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<ContractAppendixTermResponse> Terms { get; set; } = [];
}

public sealed class ContractAppendixTermResponse
{
    public int AppendixTermId { get; set; }
    public int AppendixId { get; set; }
    public int? SourceTemplateAppendixTermId { get; set; }
    public string TermCode { get; set; } = string.Empty;
    public string TermTitle { get; set; } = string.Empty;
    public string? TermTitleEn { get; set; }
    public string? TermContent { get; set; }
    public string? TermContentEn { get; set; }
    public int DisplayOrder { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractAppendixOptionResponse
{
    public int TemplateAppendixId { get; set; }
    public string AppendixCode { get; set; } = string.Empty;
    public string AppendixName { get; set; } = string.Empty;
    public string? AppendixNameEn { get; set; }
    public string? AppendixDescription { get; set; }
    public int DisplayOrder { get; set; }
}
