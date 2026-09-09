namespace ContractManagement.Domains.Interfaces.Contract;

/// <summary>
/// Applies Contract object authorization for resources that are not served by
/// ContractService itself, such as attachments and generic files.
/// </summary>
public interface IContractResourceAuthorizationService
{
    Task EnsureCanReadAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanWriteAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanManageSigningAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanManageAcceptanceAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanManagePaymentAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanCompleteAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
