namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Loại nhân viên dùng để phân quyền nghiệp vụ.
    /// Giá trị này cần khớp với EmployeeType trong database nếu có.
    /// </summary>
    public enum EmployeeType : byte
    {
        Sale = 1,
        Marketing = 2,
        AdminOfficer = 3,
        Technical = 4,
        Accountant = 5,
        Manager = 6
    }
}