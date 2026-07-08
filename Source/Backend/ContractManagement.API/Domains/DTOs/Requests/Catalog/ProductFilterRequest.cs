namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Filter danh sách sản phẩm.
    /// </summary>
    public class ProductFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Keyword { get; set; }

        public int? CategoryId { get; set; }

        public byte? Status { get; set; }
    }
}