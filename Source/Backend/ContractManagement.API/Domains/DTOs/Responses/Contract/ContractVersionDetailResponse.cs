namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Version hiện hành của hợp đồng.
    /// </summary>
    public class ContractVersionDetailResponse
    {
        public int VersionId { get; set; }

        public int VersionNo { get; set; }

        public int? SourceVersionId { get; set; }

        public int? TemplateVersionId { get; set; }

        public string? ChangeNote { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal TotalDiscount { get; set; }

        public decimal TotalVat { get; set; }

        public decimal TotalPayment { get; set; }

        /// <summary>
        /// Hash được sinh khi version bị khóa.
        /// Draft hiện tại có thể chưa có hash.
        /// </summary>
        public string? SnapshotHash { get; set; }

        public bool IsLocked { get; set; }

        public DateTime? LockedDate { get; set; }

        public int? LockedByEmployeeId { get; set; }

        public int CreatedEmployeeId { get; set; }

        public DateTime CreatedDate { get; set; }

        public string RowVersion { get; set; } = string.Empty;

        public List<ContractItemDetailResponse> Items { get; set; } = [];

        public List<ContractTermDetailResponse> Terms { get; set; } = [];

        /// <summary>
        /// Comments thuộc đúng version này, theo thứ tự thời gian tạo.
        /// Client tự dựng cây reply bằng ParentCommentId.
        /// </summary>
        public List<ContractNegotiationCommentResponse> Comments { get; set; } = [];
    }
}
