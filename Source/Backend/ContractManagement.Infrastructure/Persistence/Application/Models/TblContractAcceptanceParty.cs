namespace ContractManagement.Infrastructure.Persistence.Application.Models;
public sealed class TblContractAcceptanceParty
{
    public int AcceptancePartyId { get; set; }
    public int AcceptanceRecordId { get; set; }
    public byte PartyRole { get; set; }
    public string LegalName { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? TaxCode { get; set; }
    public string RepresentativeName { get; set; } = null!;
    public string? RepresentativeTitle { get; set; }
}
