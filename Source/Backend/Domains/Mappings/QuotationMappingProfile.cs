using AutoMapper;
using ContractManagement.Domains.DTOs.Requests;
using ContractManagement.Domains.DTOs.Responses;
using ContractManagement.Models;

namespace ContractManagement.Domains.Mappings
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
