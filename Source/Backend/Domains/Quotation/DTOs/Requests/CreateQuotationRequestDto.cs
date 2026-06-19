using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.Quotation.DTOs.Requests
{
    public class CreateQuotationRequestDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one quotation item is required")]
        public List<QuotationItemDto> QuotationItems { get; set; } = new();
    }
}