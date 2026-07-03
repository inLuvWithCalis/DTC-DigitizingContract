namespace ContractManagement.Domains.DTOs.Requests
{
    public class CustomerResponseDto
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; } 
        public string? CustomerFullName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerMobile { get; set; }
    }
}
