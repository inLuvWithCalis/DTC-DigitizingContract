using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Chi tiết đầy đủ của hợp đồng tại version hiện hành.
    /// </summary>
    public class ContractDetailResponse
    {
        public int ContractId { get; set; }

        public string? ContractCode { get; set; }

        public string ContractName { get; set; } = string.Empty;

        public string? ContractNameEn { get; set; }

        public ContractType ContractType { get; set; }

        public int? TemplateVersionId { get; set; }

        public int? ParentContractId { get; set; }

        public ContractStatus Status { get; set; }

        public DateTime? SignDate { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public ContractLanguageMode LanguageMode { get; set; }

        public bool IsLegacy { get; set; }

        public int CreatedEmployeeId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Dùng cho optimistic concurrency khi cập nhật hợp đồng sau này.
        /// </summary>
        public string RowVersion { get; set; } = string.Empty;

        public ContractCustomerSummaryResponse Customer { get; set; } = new();

        public ContractEmployeeSummaryResponse ResponsibleEmployee
        {
            get;
            set;
        } = new();

        public ContractVersionDetailResponse CurrentVersion
        {
            get;
            set;
        } = new();
    }

    /// <summary>
    /// Thông tin khách hàng cần hiển thị trong hợp đồng.
    /// Không trả password hoặc dữ liệu không cần thiết.
    /// </summary>
    public class ContractCustomerSummaryResponse
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        public string? CustomerFullName { get; set; }

        public string? CustomerCompany { get; set; }

        public string? CustomerTaxCode { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerMobile { get; set; }

        public string? CustomerAddress { get; set; }
    }

    /// <summary>
    /// Nhân viên đang chịu trách nhiệm cho hợp đồng.
    /// </summary>
    public class ContractEmployeeSummaryResponse
    {
        public int EmployeeId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? EmployeeFullName { get; set; }

        public string? EmployeeEmail { get; set; }

        public string? EmployeeMobile { get; set; }
    }
}