using ContractManagement.API.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Một Product hoặc Service được đưa vào hợp đồng.
    ///
    /// Các số tiền thành phần sẽ do backend tính toán.
    /// </summary>
    public class CreateContractItemRequest : IValidatableObject
    {
        /// <summary>
        /// 1 = Product, 2 = Service.
        /// </summary>
        [EnumDataType(typeof(ContractItemType))]
        public ContractItemType ItemType { get; set; }

        /// <summary>
        /// Product nguồn trong catalog.
        /// Null nếu là Service hoặc Product nhập ngoài.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? SourceProductId { get; set; }

        /// <summary>
        /// Service nguồn trong catalog.
        /// Null nếu là Product hoặc Service nhập ngoài.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? SourceServiceId { get; set; }

        [MaxLength(100)]
        public string? ItemCode { get; set; }

        /// <summary>
        /// Tên snapshot tiếng Việt.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Bắt buộc theo business rule nếu hợp đồng song ngữ.
        /// Service sẽ kiểm tra ở bước sau.
        /// </summary>
        [MaxLength(500)]
        public string? ItemNameEn { get; set; }

        public string? ItemDescription { get; set; }

        public string? ItemDescriptionEn { get; set; }

        [MaxLength(100)]
        public string? UnitName { get; set; }

        [MaxLength(100)]
        public string? UnitNameEn { get; set; }

        /// <summary>
        /// Số lượng phải lớn hơn 0.
        /// </summary>
        [Range(
            typeof(decimal),
            "0.0001",
            "99999999999999.9999")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá do nhân viên nhập.
        /// Không được âm.
        /// </summary>
        [Range(
            typeof(decimal),
            "0",
            "9999999999999999.99")]
        public decimal UnitPrice { get; set; }

        [EnumDataType(typeof(ContractItemDiscountMode))]
        public ContractItemDiscountMode DiscountMode { get; set; }

        [Range(typeof(decimal), "0", "100")]
        public decimal DiscountPercent { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "9999999999999999.99")]
        public decimal FixedDiscountAmount { get; set; }

        public bool IsTaxable { get; set; } = true;

        [Range(typeof(decimal), "0", "100")]
        public decimal VatPercent { get; set; }

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Kiểm tra các quy tắc liên quan giữa ItemType
        /// và Product/Service nguồn.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (ItemType == ContractItemType.Product
                && SourceServiceId.HasValue)
            {
                yield return new ValidationResult(
                    "Item loại Product không được truyền SourceServiceId.",
                    new[] { nameof(SourceServiceId) });
            }

            if (ItemType == ContractItemType.Service
                && SourceProductId.HasValue)
            {
                yield return new ValidationResult(
                    "Item loại Service không được truyền SourceProductId.",
                    new[] { nameof(SourceProductId) });
            }
        }
    }
}
