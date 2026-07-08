using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Request cập nhật sản phẩm.
    /// Không cập nhật status ở đây, status có API riêng.
    /// </summary>
    public class UpdateProductRequest
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