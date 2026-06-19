using AutoMapper;
using ContractManagement.Domains.Quotation.DTOs.Requests;
using ContractManagement.Domains.Quotation.DTOs.Responses;
using ContractManagement.Models;

namespace ContractManagement.Domains.Quotation.Mappings
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
