using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Điều kiện tìm kiếm danh sách hợp đồng.
    /// Các bộ lọc đều không bắt buộc.
    /// </summary>
    public class ContractFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Tìm theo mã hợp đồng, tên hợp đồng hoặc thông tin khách hàng.
        /// </summary>
        public string? Keyword { get; set; }

        public ContractStatus? Status { get; set; }

        public ContractType? ContractType { get; set; }

        public int? CustomerId { get; set; }
    }
}