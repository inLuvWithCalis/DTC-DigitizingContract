namespace ContractManagement.API.Domains.DTOs.Responses.LegalProfiles;

public sealed class TenantLegalProfileResponse
{
    public int TenantLegalProfileId { get; set; }

    public string LegalEntityName { get; set; } = string.Empty;

    public string TaxCode { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string RepresentativeName { get; set; } = string.Empty;

    public string RepresentativeTitle { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? FaxNumber { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankName { get; set; }

    public int CreatedByEmployeeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int UpdatedByEmployeeId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
