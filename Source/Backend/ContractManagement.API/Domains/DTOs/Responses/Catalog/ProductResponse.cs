namespace ContractManagement.API.Domains.DTOs.Responses.Catalog
{
    /// <summary>
    /// Response trả về thông tin sản phẩm.
    /// </summary>
    public class ProductResponse
    {
        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string? ProductName { get; set; }

        public int? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? ProductShortDesc { get; set; }

        public string? ProductDetails { get; set; }

        public string? ProductFeatures { get; set; }

        public string? ProductBenefit { get; set; }

        public double? ProductPrice { get; set; }

        public string? ProductSmallImage { get; set; }

        public string? ProductLargeImage { get; set; }

        public byte? LangId { get; set; }

        public byte? Status { get; set; }

        public int? ProductOrder { get; set; }

        public string? ProductTags { get; set; }

        public string? TitleBrowser { get; set; }

        public string? MetaKeyword { get; set; }

        public string? MetaDescription { get; set; }

        public DateTime? ProductCreatedDate { get; set; }
    }
}