namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public sealed class TblContractTemplateAppendix
{
    public int TemplateAppendixId { get; set; }
    public int TemplateVersionId { get; set; }
    public string AppendixCode { get; set; } = null!;
    public string AppendixName { get; set; } = null!;
    public string? AppendixNameEn { get; set; }
    public string? AppendixDescription { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSelectedByDefault { get; set; }
    public int DisplayOrder { get; set; }
    public int CreatedEmployeeId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedEmployeeId { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
