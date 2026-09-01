using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractAcceptanceEvidenceResponse
{
    public int AcceptanceEvidenceId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int VersionNo { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public int UploadedByEmployeeId { get; set; }
    public string? UploadedByEmployeeName { get; set; }
    public DateTime UploadedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractPaymentResponse
{
    public int ContractPaymentId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public int VersionNo { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
    public int? EvidenceFileId { get; set; }
    public string? EvidenceFileName { get; set; }
    public ContractPaymentStatus Status { get; set; }
    public int CreatedByEmployeeId { get; set; }
    public string? CreatedByEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VoidReason { get; set; }
    public int? VoidedByEmployeeId { get; set; }
    public string? VoidedByEmployeeName { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractCompletionBlockerResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ContractCompletionReadinessResponse
{
    public bool Signed { get; set; }
    public bool AcceptanceEvidenceAvailable { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public bool Ready { get; set; }
    public IReadOnlyList<ContractCompletionBlockerResponse> Blockers { get; set; }
        = [];
}

public sealed class ContractCompletionDetailResponse
{
    public int ContractId { get; set; }
    public ContractStatus ContractStatus { get; set; }
    public int VersionId { get; set; }
    public int VersionNo { get; set; }
    public string ContractRowVersion { get; set; } = string.Empty;
    public string VersionRowVersion { get; set; } = string.Empty;
    public ContractAcceptanceEvidenceResponse? AcceptanceEvidence { get; set; }
    public IReadOnlyList<ContractPaymentResponse> Payments { get; set; } = [];
    public ContractCompletionReadinessResponse Readiness { get; set; } = new();
}
