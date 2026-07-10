namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Filter danh sách loại dịch vụ.
    /// ServiceType là master data nhỏ, dùng phân trang để đồng bộ API từ phase này trở đi.
    /// </summary>
    public class ServiceTypeFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Keyword { get; set; }

        public byte? LangId { get; set; }
    }
}