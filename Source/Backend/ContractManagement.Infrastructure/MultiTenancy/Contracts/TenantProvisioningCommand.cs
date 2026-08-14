namespace ContractManagement.Infrastructure.MultiTenancy.Contracts;

/// <summary>
/// Command nội bộ yêu cầu Infrastructure tạo tenant.
///
/// Đây không phải HTTP request DTO.
/// </summary>
public sealed record TenantProvisioningCommand(
    string TenantCode,
    string TenantName,
    InitialManagerProvisioningCommand InitialManager,
    SecurityOperationContext SecurityContext
);
