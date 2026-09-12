namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblContractTemplateItemTableColumnLayout
{
    public int ItemTableColumnLayoutId { get; set; }
    public int TemplateVersionId { get; set; }
    public string ColumnKey { get; set; } = string.Empty;
    public byte DisplayOrder { get; set; }
    public short WidthBps { get; set; }
    public int CreatedEmployeeId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedEmployeeId { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
