namespace ContractManagement.Domains.Interfaces.File;

public interface IPrivateFileStorage
{
    Task<StoredPrivateFile> SaveAsync(
        PrivateFileSaveRequest request,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken = default);
}

public sealed record PrivateFileSaveRequest(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long DeclaredSize,
    string TenantCode,
    string ObjectType,
    int ObjectId,
    PrivateFileUploadPolicy UploadPolicy);

public sealed record StoredPrivateFile(
    string StorageKey,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string Sha256,
    DateTime CreatedAt,
    string TenantCode);

public sealed record PrivateFileUploadPolicy(
    IReadOnlyCollection<string> AllowedExtensions,
    IReadOnlyCollection<string> AllowedContentTypes,
    long MaximumSizeBytes,
    IReadOnlyDictionary<string, IReadOnlyList<byte[]>> AllowedSignatures);
