using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.CustomerInteraction
{
    public class UpdateCustomerInteractionRequest
    {
        [Required]
        [MaxLength(50)]
        public string InteractionType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? InteractionSubject { get; set; }

        public string? Content { get; set; }

        public DateTime? NextFollowUpDate { get; set; }
    }
}