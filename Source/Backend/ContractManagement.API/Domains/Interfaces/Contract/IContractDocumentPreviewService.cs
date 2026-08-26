namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractDocumentPreviewService
{
    Task<ContractDocumentPreviewResult> GenerateDocxAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ContractDocumentPreviewResult> GeneratePdfAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);
}

public sealed record ContractDocumentPreviewResult(
    byte[] Content,
    string FileName,
    string ContentType);
