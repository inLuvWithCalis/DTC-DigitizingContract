using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public interface IContractPlaceholderValueService
{
    Task<IReadOnlyDictionary<string, string>> CaptureAsync(TblContract contract, TblContractVersion version,
        bool refresh = false, CancellationToken cancellationToken = default);
}
