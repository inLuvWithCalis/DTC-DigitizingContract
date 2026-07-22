using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Cập nhật toàn bộ nội dung có thể chỉnh sửa của hợp đồng Draft.
    /// </summary>
    public class UpdateContractDraftRequest : IValidatableObject
    {
        /// <summary>
        /// RowVersion của Contract lấy từ API Get Detail.
        /// </summary>
        [Required]
        public string RowVersion { get; set; } = string.Empty;

        /// <summary>
        /// CurrentVersionId lấy từ API Get Detail.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int CurrentVersionId { get; set; }

        /// <summary>
        /// RowVersion của CurrentVersion.
        /// </summary>
        [Required]
        public string CurrentVersionRowVersion { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ContractName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ContractNameEn { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        [RegularExpression(
            "^[A-Z]{3}$",
            ErrorMessage = "CurrencyCode phải gồm đúng 3 chữ cái viết hoa.")]
        public string CurrencyCode { get; set; } = "VND";

        /// <summary>
        /// Đây là danh sách đầy đủ.
        /// Item bị bỏ khỏi danh sách sẽ bị xóa khỏi Draft.
        /// </summary>
        [Required]
        [MinLength(
            1,
            ErrorMessage = "Hợp đồng phải có ít nhất một item.")]
        public List<UpdateContractItemRequest> Items { get; set; } = [];

        /// <summary>
        /// Đây là danh sách đầy đủ các điều khoản.
        /// </summary>
        [Required]
        [MinLength(
            1,
            ErrorMessage = "Hợp đồng phải có ít nhất một điều khoản.")]
        public List<UpdateContractTermRequest> Terms { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (EffectiveDate.HasValue
                && ExpireDate.HasValue
                && ExpireDate.Value < EffectiveDate.Value)
            {
                yield return new ValidationResult(
                    "Ngày hết hạn không được trước ngày hiệu lực.",
                    new[] { nameof(ExpireDate) });
            }
        }
    }
}