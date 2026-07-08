namespace ContractManagement.API.Domains.DTOs.Responses.Department
{
    public class DepartmentResponse
    {
        public short DepartmentId { get; set; }

        public string DepartmentCode { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public DateTime? ModifiedDate { get; set; }

        public byte? Status { get; set; }

        public int? LangId { get; set; }
    }
}
