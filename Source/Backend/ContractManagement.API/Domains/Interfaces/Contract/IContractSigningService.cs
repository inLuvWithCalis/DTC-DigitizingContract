using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;

namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractSigningService
{
    Task<ContractSigningDetailResponse> GetAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractSignedEvidenceResponse> UploadAsync(
        int contractId,
        UploadContractSignedEvidenceRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractSignedEvidenceResponse> SupersedeAsync(
        int contractId,
        int signedEvidenceId,
        SupersedeContractSignedEvidenceRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);
}
