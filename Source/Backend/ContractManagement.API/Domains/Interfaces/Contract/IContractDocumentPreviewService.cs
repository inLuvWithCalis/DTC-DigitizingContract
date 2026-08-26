namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractDocumentPreviewService
{
    Task<(byte[] Content, string FileName)> GeneratePdfAsync(
        int contractId,
        int employeeId,
        bool canReadTenant,
        CancellationToken cancellationToken = default);
}
