using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.API.Domains.DTOs.Requests.Public;
using ContractManagement.API.Domains.DTOs.Responses.Public;

namespace ContractManagement.Domains.Interfaces.Contract
{
    /// <summary>
    /// Service xử lý nghiệp vụ vòng đời hợp đồng.
    /// </summary>
    public interface IContractService
    {
        /// <summary>
        /// Lấy danh sách hợp đồng mà nhân viên đang phụ trách.
        /// </summary>
        Task<PagedResult<ContractListItemResponse>> GetListAsync(
            ContractFilterRequest filter,
            int employeeId,
            bool canReadTenant = false);

        /// <summary>
        /// Lấy các hợp đồng gốc đủ điều kiện cho dropdown
        /// khi tạo hợp đồng bảo trì hoặc duy trì.
        /// </summary>
        Task<PagedResult<EligibleParentContractResponse>>
            GetEligibleParentsAsync(
                EligibleParentContractFilterRequest filter,
                int employeeId,
                bool canReadTenant = false);

        /// <summary>
        /// Tạo hợp đồng Draft cùng Version 1,
        /// item snapshot và term snapshot.
        /// </summary>
        Task<CreateContractResponse> CreateAsync(
            CreateContractRequest request,
            int createdEmployeeId);

        Task<TransferContractResponsibilityResponse>
            TransferResponsibilityAsync(
                int contractId,
                TransferContractResponsibilityRequest request,
                int actorEmployeeId);

        Task<IReadOnlyList<ContractCustomerVerificationPhoneResponse>>
            GetCustomerVerificationPhonesAsync(
                int contractId,
                int employeeId);

        Task<ContractCustomerVerificationPhoneResponse>
            UpdateCustomerVerificationPhoneAsync(
                int contractId,
                UpdateContractCustomerVerificationPhoneRequest request,
                int employeeId);

        Task<ContractCustomerAccessLinkResponse>
            CreateCustomerAccessLinkAsync(
                int contractId,
                CreateContractCustomerAccessLinkRequest request,
                int employeeId,
                string publicBaseUrl);

        Task<CurrentContractCustomerAccessLinkResponse?>
            GetCurrentCustomerAccessLinkAsync(
                int contractId,
                int employeeId);

        Task<ContractCustomerAccessLinkResponse>
            ReplaceCustomerAccessLinkAsync(
                int contractId,
                int linkId,
                ReplaceContractCustomerAccessLinkRequest request,
                int employeeId,
                string publicBaseUrl);

        Task RevokeCustomerAccessLinkAsync(
            int contractId,
            int linkId,
            RevokeContractCustomerAccessLinkRequest request,
            int employeeId);

        /// <summary>
        /// Lấy hợp đồng cùng version hiện hành, items và terms.
        /// Chỉ nhân viên đang phụ trách hợp đồng mới được xem.
        /// </summary>
        Task<ContractDetailResponse> GetDetailAsync(
            int contractId,
            int employeeId,
            bool canReadTenant = false);

        /// <summary>
        /// Cập nhật Contract, Items và Terms khi hợp đồng còn là Draft.
        /// </summary>
        Task<ContractDetailResponse> UpdateDraftAsync(
            int contractId,
            UpdateContractDraftRequest request,
            int employeeId);

        Task<ContractDetailResponse> StartNegotiationAsync(
            int contractId,
            StartContractNegotiationRequest request,
            int employeeId);

        Task<CreateContractNegotiationRoundResponse>
            CreateNegotiationRoundAsync(
                int contractId,
                CreateContractNegotiationRoundRequest request,
                int employeeId);

        Task<ContractNegotiationCommentResponse>
            CreateExternalFeedbackAsync(
                int contractId,
                CreateContractNegotiationCommentRequest request,
                int employeeId);

        Task<IReadOnlyList<ContractNegotiationCommentResponse>>
            GetRootCommentsAsync(
                int contractId,
                int employeeId,
                bool canReadTenant = false);

        Task<IReadOnlyList<ContractNegotiationCommentResponse>>
            GetCommentRepliesAsync(
                int contractId,
                int parentCommentId,
                int employeeId,
                bool canReadTenant = false);

        Task<ContractNegotiationCommentResponse>
            RecordExternalFeedbackAsync(
                int contractId,
                CreateContractNegotiationCommentRequest request,
                int employeeId);

        Task<ContractNegotiationCommentResponse>
            ResolveCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId);

        Task<ContractNegotiationCommentResponse>
            ResolveNegotiationCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId);

        Task<ContractNegotiationCommentResponse>
            ReopenCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId);

        Task<ContractNegotiationCommentResponse>
            ReopenNegotiationCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId);

        Task<CustomerPublicNegotiationCommentResponse>
            CreateCustomerCommentAsync(
                int contractId,
                int versionId,
                int customerAccessSessionId,
                CreateCustomerNegotiationCommentRequest request);

        Task<IReadOnlyList<ContractVersionHistoryResponse>>
            GetVersionHistoryAsync(
                int contractId,
                int employeeId,
                bool canReadTenant = false);

        Task<ContractVersionDetailResponse>
            GetVersionDetailAsync(
                int contractId,
                int versionId,
                int employeeId,
                bool canReadTenant = false);

        Task<SubmitContractForApprovalResponse> SubmitForApprovalAsync(
            int contractId,
            SubmitContractForApprovalRequest request,
            int employeeId);
    }
}
