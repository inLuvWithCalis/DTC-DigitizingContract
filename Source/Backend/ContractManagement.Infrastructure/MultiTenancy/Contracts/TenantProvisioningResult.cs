using ContractManagement.Infrastructure.MultiTenancy.Enums;

namespace ContractManagement.Infrastructure.MultiTenancy.Contracts;

public sealed record TenantProvisioningResult(
    int TenantId,
    string TenantCode,
    string TenantName,
    string DatabaseName,
    TenantDatabaseMode DatabaseMode,
    TenantStatus Status
);