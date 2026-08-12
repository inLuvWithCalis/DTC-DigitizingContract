using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

/// <summary>
/// Kiểm tra an toàn OOXML và placeholder của DOCX độc lập với quyền hay persistence.
/// </summary>
public interface IContractTemplateDocumentValidator
{
    Task<ContractTemplateDocumentValidationResult> ValidateAsync(
        IFormFile? file,
        CancellationToken cancellationToken = default);
}
