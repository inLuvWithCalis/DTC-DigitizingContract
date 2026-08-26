namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Hồ sơ pháp lý duy nhất của tenant hiện tại.
/// Mỗi tenant sử dụng database riêng nên bảng này không cần TenantId vật lý.
/// </summary>
public sealed class TblTenantLegalProfile
{
    public int TenantLegalProfileId { get; set; }

    public string LegalEntityName { get; set; } = string.Empty;

    public string TaxCode { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string RepresentativeName { get; set; } = string.Empty;

    public string RepresentativeTitle { get; set; } = string.Empty;

    public int CreatedByEmployeeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int UpdatedByEmployeeId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
