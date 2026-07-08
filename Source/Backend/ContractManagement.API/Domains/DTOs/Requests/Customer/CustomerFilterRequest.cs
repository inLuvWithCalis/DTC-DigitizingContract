namespace ContractManagement.API.Domains.DTOs.Requests.Customer
{
    /// <summary>
    /// Filter danh sách khách hàng.
    /// </summary>
    public class CustomerFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Keyword { get; set; }

        public byte? Status { get; set; }
    }
}
