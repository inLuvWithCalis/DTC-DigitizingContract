using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Dữ liệu tóm tắt của một hợp đồng trên màn hình danh sách.
    /// Không trả Items và Terms để response nhẹ hơn.
    /// </summary>
    public class ContractListItemResponse
    {
        public int ContractId { get; set; }

        public string? ContractCode { get; set; }

        public string ContractName { get; set; } = string.Empty;

        public ContractType ContractType { get; set; }

        public ContractStatus Status { get; set; }

        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerCompany { get; set; }

        public int ResponsibleEmployeeId { get; set; }

        public string? ResponsibleEmployeeName { get; set; }

        public int? CurrentVersionId { get; set; }

        public int? CurrentVersionNo { get; set; }

        public bool IsCurrentVersionLocked { get; set; }

        public decimal TotalAmount { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}