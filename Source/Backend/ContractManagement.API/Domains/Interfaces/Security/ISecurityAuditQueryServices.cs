using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Security;
using ContractManagement.API.Domains.DTOs.Responses.Security;

namespace ContractManagement.API.Domains.Interfaces.Security;

public interface ITenantSecurityAuditQueryService
{
    Task<PagedResult<TenantSecurityAuditResponse>> QueryAsync(
        TenantSecurityAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default);
}

public interface ICentralSecurityAuditQueryService
{
    Task<PagedResult<CentralSecurityAuditResponse>> QueryAsync(
        CentralSecurityAuditFilterRequest filter,
        int systemAdminId,
        CancellationToken cancellationToken = default);
}
