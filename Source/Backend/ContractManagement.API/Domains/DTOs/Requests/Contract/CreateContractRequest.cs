using ContractManagement.API.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Request lập một hợp đồng nháp mới.
    /// </summary>
    public class CreateContractRequest : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [EnumDataType(typeof(ContractType))]
        public ContractType ContractType { get; set; }

        /// <summary>
        /// Chỉ chấp nhận TemplateVersion đã Published.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int TemplateVersionId { get; set; }

        /// <summary>
        /// Hợp đồng nguồn đối với hợp đồng bảo trì hoặc duy trì.
        /// Không dùng trường này cho phụ lục.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? ParentContractId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ContractName { get; set; } = string.Empty;

        /// <summary>
        /// Bắt buộc theo business rule khi LanguageMode là Bilingual.
        /// </summary>
        [MaxLength(1000)]
        public string? ContractNameEn { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        /// <summary>
        /// Mã tiền tệ ISO 4217: VND, USD...
        /// </summary>
        [Required]
        [StringLength(3, MinimumLength = 3)]
        [RegularExpression(
            "^[A-Z]{3}$",
            ErrorMessage = "CurrencyCode phải gồm đúng 3 chữ cái viết hoa.")]
        public string CurrencyCode { get; set; } = "VND";

        [EnumDataType(typeof(ContractLanguageMode))]
        public ContractLanguageMode LanguageMode { get; set; }
            = ContractLanguageMode.Vietnamese;

        /// <summary>
        /// Hợp đồng phải có ít nhất một Product hoặc Service.
        /// </summary>
        [Required]
        [MinLength(
            1,
            ErrorMessage = "Hợp đồng phải có ít nhất một sản phẩm hoặc dịch vụ.")]
        public List<CreateContractItemRequest> Items { get; set; } = [];

        /// <summary>
        /// Kiểm tra những business rule liên quan giữa nhiều field.
        /// Các kiểm tra cần database sẽ được thực hiện trong ContractService.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Ngày hết hạn không được đứng trước ngày hiệu lực.
            if (EffectiveDate.HasValue
                && ExpireDate.HasValue
                && ExpireDate.Value < EffectiveDate.Value)
            {
                yield return new ValidationResult(
                    "Ngày hết hạn không được trước ngày hiệu lực.",
                    new[] { nameof(ExpireDate) });
            }

            // Hợp đồng song ngữ bắt buộc có tên tiếng Anh.
            if (LanguageMode == ContractLanguageMode.Bilingual
                && string.IsNullOrWhiteSpace(ContractNameEn))
            {
                yield return new ValidationResult(
                    "Hợp đồng song ngữ bắt buộc có tên hợp đồng tiếng Anh.",
                    new[] { nameof(ContractNameEn) });
            }

            // Từng item của hợp đồng song ngữ cũng phải có tên tiếng Anh.
            if (LanguageMode == ContractLanguageMode.Bilingual)
            {
                for (var index = 0; index < Items.Count; index++)
                {
                    if (string.IsNullOrWhiteSpace(Items[index].ItemNameEn))
                    {
                        yield return new ValidationResult(
                            $"Item thứ {index + 1} phải có tên tiếng Anh.",
                            new[] { $"{nameof(Items)}[{index}].ItemNameEn" });
                    }
                }
            }

            // Hợp đồng cung cấp phần mềm là hợp đồng gốc.
            if (ContractType == ContractType.SoftwareSupply
                && ParentContractId.HasValue)
            {
                yield return new ValidationResult(
                    "Hợp đồng cung cấp phần mềm không được có hợp đồng nguồn.",
                    new[] { nameof(ParentContractId) });
            }

            // Bảo trì và duy trì phải phát sinh từ hợp đồng cung cấp phần mềm.
            if (ContractType is ContractType.SoftwareMaintenance
                    or ContractType.SoftwareUpkeep
                && !ParentContractId.HasValue)
            {
                yield return new ValidationResult(
                    "Hợp đồng bảo trì hoặc duy trì bắt buộc có hợp đồng nguồn.",
                    new[] { nameof(ParentContractId) });
            }
        }
    }
}