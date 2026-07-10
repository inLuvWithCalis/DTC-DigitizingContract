namespace ContractManagement.API.Domains.DTOs.Requests.Catalog
{
    /// <summary>
    /// Filter danh sách dịch vụ.
    /// </summary>
    public class ServiceFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Keyword { get; set; }

        public byte? ServiceTypeId { get; set; }

        public byte? Status { get; set; }

        public int? LangId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
