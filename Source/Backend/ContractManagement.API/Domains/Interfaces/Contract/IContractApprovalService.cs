using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;

namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractApprovalService
{
    Task<PagedResult<ContractApprovalRequestResponse>> GetInboxAsync(
        ContractApprovalInboxFilterRequest filter,
        int managerEmployeeId,
        CancellationToken cancellationToken = default);

    Task<ContractApprovalDetailResponse> GetDetailAsync(
        int approvalRequestId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractApprovalRequestResponse>> GetContractHistoryAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractApprovalActionResponse> DecideAsync(
        int approvalRequestId,
        ApprovalRequestStatus decision,
        ContractApprovalDecisionRequest request,
        int managerEmployeeId,
        CancellationToken cancellationToken = default);

    Task<ContractApprovalBulkDecisionResponse> DecideBulkAsync(
        ContractApprovalBulkDecisionRequest request,
        int managerEmployeeId,
        CancellationToken cancellationToken = default);

    Task<ContractApprovalActionResponse> WithdrawAsync(
        int approvalRequestId,
        WithdrawContractApprovalRequest request,
        int ownerEmployeeId,
        CancellationToken cancellationToken = default);
}
