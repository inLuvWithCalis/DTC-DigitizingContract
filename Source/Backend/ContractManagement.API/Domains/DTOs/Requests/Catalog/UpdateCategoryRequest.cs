using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Request cập nhật danh mục sản phẩm.
    /// </summary>
    public class UpdateCategoryRequest
    {
        [Required]
        [MaxLength(500)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CategoryShortDesc { get; set; }

        public byte? CategoryOrder { get; set; }

        public byte? CategoryParentId { get; set; }

        public int? LangId { get; set; }

        [MaxLength(50)]
        public string? Image { get; set; }
    }
}