using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.DTOs.Requests.Quotation
{
    public class UpdateQuotationRequestDto
    {
        [Required(ErrorMessage = "Quotation status is required.")]
        public string QuotationStatus { get; set; } = string.Empty;
    }
}
