namespace ContractManagement.Infrastructure.MultiTenancy.Contracts;

/// <summary>
/// The first Manager is supplied only when a tenant is provisioned.
/// Role and active status are intentionally not client-controlled.
/// </summary>
public sealed record InitialManagerProvisioningCommand(
    string? EmployeeCode,
    string EmployeeAccount,
    string EmployeePassword,
    string EmployeeFullName,
    string? EmployeeMobile,
    string? EmployeeEmail);
