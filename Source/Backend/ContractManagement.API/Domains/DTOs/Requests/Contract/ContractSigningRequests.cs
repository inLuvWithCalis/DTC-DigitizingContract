namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public class UploadContractSignedEvidenceRequest
{
    public IFormFile File { get; set; } = null!;

    public int CurrentVersionId { get; set; }

    public string ContractRowVersion { get; set; } = string.Empty;

    public string VersionRowVersion { get; set; } = string.Empty;

    public string ProviderSignerName { get; set; } = string.Empty;

    public string ProviderSignerTitle { get; set; } = string.Empty;

    public DateTime ProviderSigningDate { get; set; }

    public string CustomerSignerName { get; set; } = string.Empty;

    public string CustomerSignerTitle { get; set; } = string.Empty;

    public DateTime CustomerSigningDate { get; set; }
}

public sealed class SupersedeContractSignedEvidenceRequest
    : UploadContractSignedEvidenceRequest
{
    public string EvidenceRowVersion { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
