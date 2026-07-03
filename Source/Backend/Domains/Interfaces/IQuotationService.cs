using ContractManagement.Domains.DTOs.Requests;
using ContractManagement.Domains.DTOs.Responses;

namespace ContractManagement.Domains.Interfaces
{
    public interface IQuotationService
    {
        Task<QuotationResponseDto> CreateQuotationAsync(CreateQuotationRequestDto request, int currentEmployeeId);

        Task<QuotationResponseDto> GetQuotationByIdAsync(int quotationId);
        Task<List<QuotationResponseDto>> GetAllQuotationsAsync();
        Task<bool> UpdateQuotationAsync(int quotationId, UpdateQuotationRequestDto request);
        Task<bool> DeleteQuotationAsync(int quotationId);
    }
}
