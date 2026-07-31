using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;

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
            int employeeId);

        /// <summary>
        /// Lấy các hợp đồng gốc đủ điều kiện cho dropdown
        /// khi tạo hợp đồng bảo trì hoặc duy trì.
        /// </summary>
        Task<PagedResult<EligibleParentContractResponse>>
            GetEligibleParentsAsync(
                EligibleParentContractFilterRequest filter,
                int employeeId);

        /// <summary>
        /// Tạo hợp đồng Draft cùng Version 1,
        /// item snapshot và term snapshot.
        /// </summary>
        Task<CreateContractResponse> CreateAsync(
            CreateContractRequest request,
            int createdEmployeeId);

        /// <summary>
        /// Lấy hợp đồng cùng version hiện hành, items và terms.
        /// Chỉ nhân viên đang phụ trách hợp đồng mới được xem.
        /// </summary>
        Task<ContractDetailResponse> GetDetailAsync(
            int contractId,
            int employeeId);

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

        Task<SubmitContractForApprovalResponse> SubmitForApprovalAsync(
            int contractId,
            SubmitContractForApprovalRequest request,
            int employeeId);
    }
}