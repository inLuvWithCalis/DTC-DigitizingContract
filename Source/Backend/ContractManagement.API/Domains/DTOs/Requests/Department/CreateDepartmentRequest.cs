using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Department
{
    public class CreateDepartmentRequest
    {
        [Required]
        [MaxLength(20)]
        public string DepartmentCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DepartmentName { get; set; } = string.Empty;

        public int? LangId { get; set; }
    }
}