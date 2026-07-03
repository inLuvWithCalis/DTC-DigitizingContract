using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.DTOs.Requests
{
    public class UpdateQuotationRequestDto
    {
        [Required(ErrorMessage = "Quotation status is required.")]
        public string QuotationStatus { get; set; }
    }
}
