using System.Security.Cryptography;
using ContractManagement.Domains.Interfaces.File;

namespace ContractManagement.Tests.TestDoubles;

public sealed class InMemoryPrivateFileStorage : IPrivateFileStorage
{
    private readonly Dictionary<string, (byte[] Content, string ContentType)> _files = [];

    public PrivateFileSaveRequest? LastSaveRequest { get; private set; }

    public IReadOnlyCollection<string> DeletedStorageKeys => _deletedStorageKeys;

    private readonly List<string> _deletedStorageKeys = [];

    public async Task<StoredPrivateFile> SaveAsync(
        PrivateFileSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        LastSaveRequest = request;
        await using var memory = new MemoryStream();
        await request.Content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        var storageKey = string.Join(
            '/',
            request.TenantCode,
            request.ObjectType,
            request.ObjectId,
            $"{Guid.NewGuid():N}{extension}");
        _files[storageKey] = (bytes, request.ContentType);
        return new StoredPrivateFile(
            storageKey,
            request.OriginalFileName,
            request.ContentType,
            bytes.LongLength,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            DateTime.UtcNow,
            request.TenantCode);
    }

    public Task<Stream> OpenReadAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(storageKey, out var file)
            || !storageKey.StartsWith($"{tenantCode}/", StringComparison.Ordinal))
        {
            throw new FileNotFoundException();
        }

        return Task.FromResult<Stream>(new MemoryStream(file.Content, writable: false));
    }

    public Task DeleteAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (storageKey.StartsWith($"{tenantCode}/", StringComparison.Ordinal))
        {
            _files.Remove(storageKey);
            _deletedStorageKeys.Add(storageKey);
        }

        return Task.CompletedTask;
    }
}
