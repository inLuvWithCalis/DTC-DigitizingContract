using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

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

    public DateTime? PasswordChangedAt { get; set; }

    public bool MustChangePassword { get; set; }

    public int SessionVersion { get; set; } = 1;

    public string? AvatarStorageKey { get; set; }

    public string? AvatarContentType { get; set; }

    public long? AvatarFileSize { get; set; }

    public string? AvatarSha256 { get; set; }

    public DateTime? AvatarUpdatedAt { get; set; }

    public string? CoverStorageKey { get; set; }

    public string? CoverContentType { get; set; }

    public long? CoverFileSize { get; set; }

    public string? CoverSha256 { get; set; }

    public DateTime? CoverUpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
