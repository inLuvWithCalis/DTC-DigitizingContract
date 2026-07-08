using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Department
{
    public class UpdateDepartmentRequest
    {
        [Required]
        [MaxLength(200)]
        public string DepartmentName { get; set; } = string.Empty;

        public int? LangId { get; set; }
    }
}
