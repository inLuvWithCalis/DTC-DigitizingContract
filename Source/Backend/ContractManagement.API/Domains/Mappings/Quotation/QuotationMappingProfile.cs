using AutoMapper;
using ContractManagement.Domains.DTOs.Requests.Quotation;
using ContractManagement.Domains.DTOs.Responses.Quotation;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Mappings.Quotation
{
    public class QuotationMappingProfile : Profile
    {
        public QuotationMappingProfile()
        {
            // Map Request -> Entity
            CreateMap<CreateQuotationRequestDto, TblQuotation>();
            CreateMap<QuotationItemDto, TblQuotationDetail>()
                .ForMember(dest => dest.Amount, opt => opt.Ignore());

            // Map Entity -> Response
            CreateMap<TblQuotation, QuotationResponseDto>();
            CreateMap<TblQuotationDetail, QuotationResponseDto.ItemResponse>();
        }
    }
}
