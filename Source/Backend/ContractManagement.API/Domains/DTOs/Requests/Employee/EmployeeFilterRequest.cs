namespace ContractManagement.API.Domains.DTOs.Requests.Employee
{
    /// <summary>
    /// Filter danh sách nhân viên.
    /// </summary>
    public class EmployeeFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Keyword { get; set; }

        public int? CategoryId { get; set; }

        public byte? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
