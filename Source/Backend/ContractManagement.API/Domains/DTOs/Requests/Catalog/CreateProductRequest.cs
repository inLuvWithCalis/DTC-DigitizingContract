using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Request tạo sản phẩm mới.
    /// Product dùng làm dữ liệu nền cho báo giá, đơn hàng, hợp đồng.
    /// </summary>
    public class CreateProductRequest
    {
        [MaxLength(20)]
        public string? ProductCode { get; set; }

        [Required]
        [MaxLength(500)]
        public string ProductName { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        [MaxLength(2000)]
        public string? ProductShortDesc { get; set; }

        public string? ProductDetails { get; set; }

        public string? ProductFeatures { get; set; }

        public string? ProductBenefit { get; set; }

        public double? ProductPrice { get; set; }

        [MaxLength(500)]
        public string? ProductSmallImage { get; set; }

        [MaxLength(500)]
        public string? ProductLargeImage { get; set; }

        public byte? LangId { get; set; }

        public int? ProductOrder { get; set; }

        [MaxLength(500)]
        public string? ProductTags { get; set; }

        [MaxLength(500)]
        public string? TitleBrowser { get; set; }

        [MaxLength(2000)]
        public string? MetaKeyword { get; set; }

        [MaxLength(2000)]
        public string? MetaDescription { get; set; }
    }
}