using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblEmployee
{
    public int EmployeeId { get; set; }

    public string? EmployeeCode { get; set; }

    public string? EmployeeAccount { get; set; }

    public string? EmployeePassword { get; set; }

    public string? EmployeeFullName { get; set; }

    public short? TitleId { get; set; }

    public DateTime? EmployeeBirthDate { get; set; }

    public string? MaritalStatus { get; set; }

    public string? Gender { get; set; }

    public string? EmployeeMobile { get; set; }

    public string? EmployeePhone { get; set; }

    public string? EmployeeEmail { get; set; }

    public string? EmployeeAddress { get; set; }

    public int? UserCreated { get; set; }

    public int? UserModified { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public DateTime? HireDate { get; set; }

    public byte? Status { get; set; }

    public int? DepartmentId { get; set; }

    public string? Others { get; set; }

    public string? DefaultPage { get; set; }

    public string? EmployeeImageIcon { get; set; }

    public byte? EmployeeType { get; set; }

    public string? UserRoles { get; set; }

    public int? WorkTypeId { get; set; }
}
