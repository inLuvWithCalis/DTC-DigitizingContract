namespace ContractManagement.API.Domains.DTOs.Responses.Catalog
{
    /// <summary>
    /// Response trả về thông tin danh mục sản phẩm.
    /// </summary>
    public class CategoryResponse
    {
        public byte CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? CategoryShortDesc { get; set; }

        public byte? CategoryOrder { get; set; }

        public byte? CategoryParentId { get; set; }

        public int? LangId { get; set; }

        public string? Image { get; set; }

        public int ProductCount { get; set; }
    }
}