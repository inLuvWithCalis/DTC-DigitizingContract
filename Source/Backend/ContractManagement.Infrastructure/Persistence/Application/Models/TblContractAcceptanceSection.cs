namespace ContractManagement.Infrastructure.Persistence.Application.Models;
public sealed class TblContractAcceptanceSection
{
    public int AcceptanceSectionId { get; set; }
    public int AcceptanceRecordId { get; set; }
    public string SectionCode { get; set; } = null!;
    public string TitleVi { get; set; } = null!;
    public string? TitleEn { get; set; }
    public string? ContentVi { get; set; }
    public string? ContentEn { get; set; }
    public int DisplayOrder { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
