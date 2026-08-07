using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;

namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractAuditQueryService
{
    Task<PagedResult<ContractAuditResponse>> QueryAsync(
        ContractAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default);
}
