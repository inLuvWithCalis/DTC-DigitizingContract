using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public interface IContractTemplateService
{
    Task<IReadOnlyList<AvailableContractTemplateVersionResponse>>
        ListAvailableAsync(CancellationToken cancellationToken = default);

    Task<SoftwareSupplyPlaceholderCatalogResponse> GetPlaceholderCatalogAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ContractTemplateResponse>> ListAsync(
        ContractTemplateFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateDetailResponse> GetAsync(
        int templateId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateDetailResponse> CreateAsync(
        CreateContractTemplateRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateDetailResponse> UpdateAsync(
        int templateId,
        UpdateContractTemplateRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateVersionDetailResponse> GetVersionAsync(
        int versionId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateVersionDetailResponse> CopyVersionAsync(
        int sourceVersionId,
        CopyContractTemplateVersionRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateVersionDetailResponse> UploadDocumentAsync(
        int versionId,
        UploadContractTemplateDocumentRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplatePreviewResponse> GeneratePreviewAsync(
        int versionId,
        GenerateContractTemplatePreviewRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string FileName)> DownloadPreviewAsync(
        int versionId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateVersionDetailResponse> PublishAsync(
        int versionId,
        PublishContractTemplateVersionRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateVersionDetailResponse> RetireAsync(
        int versionId,
        RetireContractTemplateVersionRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string FileName)> DownloadPublishedPreviewPdfAsync(
        int versionId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateTermResponse> AddTermAsync(
        int versionId,
        CreateContractTemplateTermRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateTermResponse> UpdateTermAsync(
        int versionId,
        int termId,
        UpdateContractTemplateTermRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task DeleteTermAsync(
        int versionId,
        int termId,
        DeleteContractTemplateTermRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractTemplateVersionDetailResponse> ReorderTermsAsync(
        int versionId,
        ReorderContractTemplateTermsRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);
}
