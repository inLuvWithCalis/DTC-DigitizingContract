using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Employee
{
    /// <summary>
    /// Request cập nhật thông tin nhân viên.
    /// Không cho đổi password ở đây, password có API riêng.
    /// </summary>
    public class UpdateEmployeeRequest
    {
        [MaxLength(30)]
        public string? EmployeeCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string EmployeeFullName { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? EmployeeMobile { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? EmployeeEmail { get; set; }

        public int? DepartmentId { get; set; }

        [Range(1, 6)]
        public byte EmployeeType { get; set; }

        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }
}
