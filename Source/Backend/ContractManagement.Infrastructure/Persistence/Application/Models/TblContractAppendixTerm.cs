namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractAppendixTerm
{
    public int AppendixTermId { get; set; }
    public int AppendixId { get; set; }
    public int? SourceTemplateAppendixTermId { get; set; }
    public string TermCode { get; set; } = null!;
    public string TermTitle { get; set; } = null!;
    public string? TermTitleEn { get; set; }
    public string? TermContent { get; set; }
    public string? TermContentEn { get; set; }
    public int DisplayOrder { get; set; }
    public int CreatedEmployeeId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedEmployeeId { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
