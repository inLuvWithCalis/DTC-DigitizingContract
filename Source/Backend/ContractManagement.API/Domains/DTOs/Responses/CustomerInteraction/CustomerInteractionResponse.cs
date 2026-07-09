namespace ContractManagement.API.Domains.DTOs.Responses.CustomerInteraction
{
    public class CustomerInteractionResponse
    {
        public int InteractionId { get; set; }

        public int CustomerId { get; set; }

        public int EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public DateTime InteractionDate { get; set; }

        public string InteractionType { get; set; } = string.Empty;

        public string? InteractionSubject { get; set; }

        public string? Content { get; set; }

        public DateTime? NextFollowUpDate { get; set; }
    }
}