namespace ContractManagement.Domains.DTOs.Requests.Authentication
{
    public class LoginRequest
    {
        public string AccountName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
