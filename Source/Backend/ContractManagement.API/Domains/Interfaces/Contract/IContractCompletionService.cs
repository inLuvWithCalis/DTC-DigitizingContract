using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;

namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractCompletionService
{
    Task<ContractCompletionDetailResponse> GetAsync(
        int contractId, int employeeId, CancellationToken cancellationToken = default);
    Task<ContractCompletionReadinessResponse> GetReadinessAsync(
        int contractId, int employeeId, CancellationToken cancellationToken = default);
    Task<ContractAcceptanceEvidenceResponse> UploadAcceptanceAsync(
        int contractId, UploadContractAcceptanceEvidenceRequest request,
        int employeeId, CancellationToken cancellationToken = default);
    Task<ContractPaymentResponse> AddPaymentAsync(
        int contractId, AddContractPaymentRequest request,
        int employeeId, CancellationToken cancellationToken = default);
    Task<ContractPaymentResponse> VoidPaymentAsync(
        int contractId, int paymentId, VoidContractPaymentRequest request,
        int employeeId, CancellationToken cancellationToken = default);
    Task<ContractCompletionDetailResponse> CompleteAsync(
        int contractId, CompleteContractRequest request,
        int employeeId, CancellationToken cancellationToken = default);
}
