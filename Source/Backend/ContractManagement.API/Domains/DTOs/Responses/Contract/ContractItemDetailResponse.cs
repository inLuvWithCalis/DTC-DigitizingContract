using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Product/Service snapshot của version hiện hành.
    /// </summary>
    public class ContractItemDetailResponse
    {
        public int ContractItemId { get; set; }

        public ContractItemType ItemType { get; set; }

        public int? SourceProductId { get; set; }

        public int? SourceServiceId { get; set; }

        public string? ItemCode { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string? ItemNameEn { get; set; }

        public string? ItemDescription { get; set; }

        public string? ItemDescriptionEn { get; set; }

        public string? UnitName { get; set; }

        public string? UnitNameEn { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineSubtotal { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatPercent { get; set; }

        public decimal VatAmount { get; set; }

        public decimal LineTotal { get; set; }

        public int DisplayOrder { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}