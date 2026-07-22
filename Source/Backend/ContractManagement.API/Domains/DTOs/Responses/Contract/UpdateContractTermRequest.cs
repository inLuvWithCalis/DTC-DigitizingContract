using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Điều khoản được gửi khi cập nhật Draft.
    /// </summary>
    public class UpdateContractTermRequest
    {
        [Range(1, int.MaxValue)]
        public int? TermId { get; set; }

        /// <summary>
        /// Bắt buộc khi TermId có giá trị.
        /// </summary>
        public string? RowVersion { get; set; }

        [Required]
        [MaxLength(100)]
        public string TermCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string TermTitle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? TermTitleEn { get; set; }

        public string? TermContent { get; set; }

        public string? TermContentEn { get; set; }

        public bool IsNegotiable { get; set; }

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }
    }
}