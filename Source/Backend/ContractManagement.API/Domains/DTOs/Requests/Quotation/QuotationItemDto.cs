using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.DTOs.Requests.Quotation
{
    public class QuotationItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be a positive number")]
        public decimal UnitPrice { get; set; }
    }
}