namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Filter danh sách danh mục sản phẩm.
    /// Category là master data nhỏ, nhưng hỗ trợ keyword search và phân trang.
    /// </summary>
    public class CategoryFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Keyword { get; set; }
    }
}
