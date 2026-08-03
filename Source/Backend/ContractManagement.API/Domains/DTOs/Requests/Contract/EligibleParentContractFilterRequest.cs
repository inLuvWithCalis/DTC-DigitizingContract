using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Điều kiện tìm hợp đồng gốc cho hợp đồng bảo trì/duy trì.
    /// </summary>
    public class EligibleParentContractFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Tìm theo mã hoặc tên hợp đồng gốc.
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Chỉ tìm hợp đồng gốc của đúng khách hàng đang được chọn.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Loại hợp đồng sắp tạo: bảo trì hoặc duy trì.
        /// </summary>
        public ContractType TargetContractType { get; set; }
    }
}