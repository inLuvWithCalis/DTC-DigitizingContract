using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class UploadContractAcceptanceEvidenceRequest
{
    public IFormFile File { get; set; } = null!;
    public int CurrentVersionId { get; set; }
    public string ContractRowVersion { get; set; } = string.Empty;
    public string VersionRowVersion { get; set; } = string.Empty;
}

public sealed class AddContractPaymentRequest
{
    public IFormFile? EvidenceFile { get; set; }
    public int CurrentVersionId { get; set; }
    public string ContractRowVersion { get; set; } = string.Empty;
    public string VersionRowVersion { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
}

public sealed class VoidContractPaymentRequest
{
    public string ContractRowVersion { get; set; } = string.Empty;
    public string VersionRowVersion { get; set; } = string.Empty;
    public string PaymentRowVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class CompleteContractRequest
{
    public int CurrentVersionId { get; set; }
    public string ContractRowVersion { get; set; } = string.Empty;
    public string VersionRowVersion { get; set; } = string.Empty;
}
