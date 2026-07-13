using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Request tạo loại dịch vụ.
    /// Ví dụ: Tư vấn, Triển khai, Bảo trì, Hosting.
    /// </summary>
    public class CreateServiceTypeRequest
    {
        [Required]
        [MaxLength(200)]
        public string ServiceTypeName { get; set; } = string.Empty;

        public byte? LangId { get; set; }
    }
}