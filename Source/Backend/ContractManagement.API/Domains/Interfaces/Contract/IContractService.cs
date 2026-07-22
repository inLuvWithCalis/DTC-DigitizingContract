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
        /// Tạo hợp đồng Draft cùng Version 1,
        /// item snapshot và term snapshot.
        /// </summary>
        Task<CreateContractResponse> CreateAsync(
            CreateContractRequest request,
            int createdEmployeeId);
    }
}