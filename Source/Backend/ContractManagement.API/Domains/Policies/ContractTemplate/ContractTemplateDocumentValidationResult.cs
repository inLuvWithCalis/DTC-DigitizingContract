namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Kết quả validator. Bytes chỉ sống trong request để storage và hash dùng đúng
/// payload vừa được kiểm tra; chúng không được ghi vào validation/audit.
/// </summary>
public sealed record ContractTemplateDocumentValidationResult(
    bool IsTechnicallyAccepted,
    bool IsCatalogValid,
    IReadOnlyCollection<string> RecognizedPlaceholderKeys,
    string? FailureCode,
    string? ValidationMessage,
    string FileExtension,
    long FileSizeBytes,
    byte[]? DocumentBytes)
{
    public static ContractTemplateDocumentValidationResult RejectTechnical(
        string failureCode,
        string fileExtension,
        long fileSizeBytes) => new(
            IsTechnicallyAccepted: false,
            IsCatalogValid: false,
            RecognizedPlaceholderKeys: Array.Empty<string>(),
            FailureCode: failureCode,
            ValidationMessage: null,
            FileExtension: fileExtension,
            FileSizeBytes: fileSizeBytes,
            DocumentBytes: null);
}
