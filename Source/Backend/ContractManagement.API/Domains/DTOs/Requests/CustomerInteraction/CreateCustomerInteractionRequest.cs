using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.CustomerInteraction
{
    public class CreateCustomerInteractionRequest
    {
        /// <summary>
        /// Loại tương tác: Call, Email, Meeting, Zalo.
        /// MVP chỉ hỗ trợ 4 loại tương tác này, nếu muốn thêm loại tương tác khác thì cần nâng cấp hệ thống => thêm enum mới và validate trong service.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string InteractionType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? InteractionSubject { get; set; }

        public string? Content { get; set; }

        public DateTime? NextFollowUpDate { get; set; }
    }
}