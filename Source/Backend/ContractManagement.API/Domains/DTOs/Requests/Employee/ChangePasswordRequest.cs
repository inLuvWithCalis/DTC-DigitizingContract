using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Employee
{
    /// <summary>
    /// Request đổi mật khẩu nhân viên.
    /// Admin có thể dùng để reset hoặc nhân viên đổi mật khẩu sau này(old password).
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;
    }
}