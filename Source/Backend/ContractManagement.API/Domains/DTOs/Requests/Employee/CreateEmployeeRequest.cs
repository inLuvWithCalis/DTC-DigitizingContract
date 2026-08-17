using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Employee
{
    /// <summary>
    /// Request tạo nhân viên mới.
    /// Admin/Manager dùng API này để tạo account đăng nhập cho nhân viên.
    /// </summary>
    public class CreateEmployeeRequest
    {
        [MaxLength(30)]
        public string? EmployeeCode { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeAccount { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string EmployeePassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EmployeeFullName { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? EmployeeMobile { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? EmployeeEmail { get; set; }

        public int? DepartmentId { get; set; }

        /// <summary>
        /// 1 = Sale
        /// 2 = Marketing
        /// 3 = AdminOfficer
        /// 4 = Technical
        /// 5 = Accountant
        /// 6 = Manager
        /// </summary>
        [Range(1, 6)]
        public byte EmployeeType { get; set; }
    }
}
