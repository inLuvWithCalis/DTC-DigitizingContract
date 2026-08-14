namespace ContractManagement.Domains.Interfaces.File;

/// <summary>
/// Resolves a generic file to its owning object and applies that object's
/// authorization policy before a file can be listed, downloaded, uploaded or deleted.
/// </summary>
public interface IFileResourceAuthorizationService
{
    Task EnsureCanReadByObjectAsync(
        string objectType,
        int objectId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanWriteByObjectAsync(
        string objectType,
        int objectId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanReadFileAsync(
        int fileId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task EnsureCanDeleteFileAsync(
        int fileId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
