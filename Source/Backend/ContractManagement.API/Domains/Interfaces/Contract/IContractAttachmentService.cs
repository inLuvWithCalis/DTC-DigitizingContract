using ContractManagement.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.DTOs.Responses.Contract;

namespace ContractManagement.Domains.Interfaces.Contract
{
    /// <summary>
    /// Service quản lý file đính kèm riêng cho hợp đồng.
    /// </summary>
    public interface IContractAttachmentService
    {
        Task<ContractAttachmentResponse> UploadAsync(
            int contractId,
            UploadContractAttachmentRequest request,
            int uploadedBy);

        Task<List<ContractAttachmentResponse>> GetByContractAsync(
            int contractId,
            int employeeId);

        Task<(Stream Stream, string FileName)?> DownloadAsync(
            int contractId,
            int attachmentId,
            int employeeId);

        Task DeleteAsync(int contractId, int attachmentId, int employeeId);
    }
}
