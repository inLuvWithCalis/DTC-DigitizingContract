using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Customer
{
    /// <summary>
    /// Request tạo khách hàng mới.
    /// Khách hàng sau này sẽ được gắn với hợp đồng qua CustomerId.
    /// </summary>
    public class CreateCustomerRequest
    {
        [MaxLength(30)]
        public string? CustomerCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerFullName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CustomerCompany { get; set; }

        [MaxLength(50)]
        [EmailAddress]
        public string? CustomerEmail { get; set; }

        [MaxLength(15)]
        public string? CustomerMobile { get; set; }

        [MaxLength(15)]
        public string? CustomerPhone { get; set; }

        [MaxLength(30)]
        public string? CustomerTaxCode { get; set; }

        [MaxLength(2000)]
        public string? CustomerAddress { get; set; }

        [MaxLength(1000)]
        public string? CustomerCity { get; set; }

        [MaxLength(200)]
        public string? CustomerCountry { get; set; }

        [MaxLength(500)]
        public string? CustomerWebsite { get; set; }

        [MaxLength(2000)]
        public string? CustomerNotes { get; set; }
    }
}