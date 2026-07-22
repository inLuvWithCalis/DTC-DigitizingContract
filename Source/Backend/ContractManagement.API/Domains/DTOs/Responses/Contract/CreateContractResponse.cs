using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Kết quả trả về sau khi tạo thành công hợp đồng nháp.
    ///
    /// Chi tiết đầy đủ sẽ được lấy bằng API Get Contract By Id sau này.
    /// </summary>
    public class CreateContractResponse
    {
        /// <summary>
        /// ID của hợp đồng vừa tạo.
        /// </summary>
        public int ContractId { get; set; }

        /// <summary>
        /// Mã hợp đồng do backend sinh.
        /// </summary>
        public string ContractCode { get; set; } = string.Empty;

        public string ContractName { get; set; } = string.Empty;

        /// <summary>
        /// Hợp đồng mới luôn bắt đầu ở trạng thái Draft.
        /// </summary>
        public ContractStatus Status { get; set; }

        /// <summary>
        /// ID của Version 1 vừa được tạo.
        /// </summary>
        public int CurrentVersionId { get; set; }

        /// <summary>
        /// Hợp đồng mới luôn bắt đầu từ Version 1.
        /// </summary>
        public int VersionNo { get; set; }

        public int CustomerId { get; set; }

        public ContractType ContractType { get; set; }

        public int TemplateVersionId { get; set; }

        /// <summary>
        /// Tổng tiền sau chiết khấu và VAT,
        /// được backend tính từ toàn bộ ContractItem.
        /// </summary>
        public decimal TotalAmount { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public ContractLanguageMode LanguageMode { get; set; }

        /// <summary>
        /// Nhân viên tạo đồng thời là người phụ trách ban đầu.
        /// </summary>
        public int EmployeeId { get; set; }

        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Số item đã được snapshot vào Version 1.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Số điều khoản được snapshot từ template.
        /// </summary>
        public int TermCount { get; set; }
    }
}