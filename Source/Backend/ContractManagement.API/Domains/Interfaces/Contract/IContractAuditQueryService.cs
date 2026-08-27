using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;

namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractAuditQueryService
{
    Task<ContractAuditCursorPageResponse> QueryAsync(
        ContractAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractAuditExportFile> ExportCsvAsync(
        ContractAuditFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default);
}

public sealed record ContractAuditExportFile(
    byte[] Content,
    string FileName);
