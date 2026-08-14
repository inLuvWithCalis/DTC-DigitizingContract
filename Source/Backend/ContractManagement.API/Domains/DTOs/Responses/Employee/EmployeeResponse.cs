namespace ContractManagement.API.Domains.DTOs.Responses.Employee
{
    /// <summary>
    /// Response trả về thông tin nhân viên.
    /// </summary>
    public class EmployeeResponse
    {
        public int EmployeeId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? EmployeeAccount { get; set; }

        public string? EmployeeFullName { get; set; }

        public string? EmployeeMobile { get; set; }

        public string? EmployeeEmail { get; set; }

        public int? DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        public byte? EmployeeType { get; set; }

        public string EmployeeTypeName { get; set; } = string.Empty;

        public byte? Status { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}
