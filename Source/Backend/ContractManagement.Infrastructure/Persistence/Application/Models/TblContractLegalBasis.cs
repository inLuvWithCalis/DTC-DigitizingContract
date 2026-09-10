namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Snapshot căn cứ pháp lý thuộc riêng một phiên bản hợp đồng.
/// </summary>
public partial class TblContractLegalBasis
{
    public int LegalBasisId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public int? SourceTemplateLegalBasisId { get; set; }

    public string BasisCode { get; set; } = null!;

    public string ContentVi { get; set; } = null!;

    public string? ContentEn { get; set; }

    public int DisplayOrder { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
