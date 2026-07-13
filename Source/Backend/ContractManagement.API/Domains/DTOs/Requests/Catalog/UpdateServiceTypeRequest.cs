using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Request cập nhật loại dịch vụ.
    /// </summary>
    public class UpdateServiceTypeRequest
    {
        [Required]
        [MaxLength(200)]
        public string ServiceTypeName { get; set; } = string.Empty;

        public byte? LangId { get; set; }
    }
}