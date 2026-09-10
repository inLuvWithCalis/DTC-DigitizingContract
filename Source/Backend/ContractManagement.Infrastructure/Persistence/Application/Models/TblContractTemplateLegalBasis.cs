namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Căn cứ pháp lý được cấu hình cho một template version.
/// Khi tạo hợp đồng, dữ liệu được sao chép sang TblContractLegalBasis.
/// </summary>
public partial class TblContractTemplateLegalBasis
{
    public int TemplateLegalBasisId { get; set; }

    public int TemplateVersionId { get; set; }

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
