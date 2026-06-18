using ContractManagement.Domains.Quotation.DTOs.Requests;
using ContractManagement.Domains.Quotation.DTOs.Responses;

namespace ContractManagement.Domains.Quotation.Interfaces
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
