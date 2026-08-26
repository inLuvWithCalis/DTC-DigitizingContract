using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ContractManagement.Domains.Interfaces.File;
using Microsoft.Extensions.Options;

namespace ContractManagement.Domains.Services.File;

public sealed partial class LocalPrivateFileStorage : IPrivateFileStorage
{
    private readonly string _rootPath;

    public LocalPrivateFileStorage(
        IOptions<PrivateFileStorageOptions> options,
        IWebHostEnvironment environment)
    {
        var configuredRoot = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            throw new InvalidOperationException(
                "PrivateFileStorage:RootPath chưa được cấu hình.");
        }

        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(environment.ContentRootPath, configuredRoot));

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        if (IsWithinRoot(_rootPath, Path.GetFullPath(webRoot)))
        {
            throw new InvalidOperationException(
                "Private storage không được đặt trong wwwroot.");
        }

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredPrivateFile> SaveAsync(
        PrivateFileSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.UploadPolicy);

        var tenantCode = ValidateSegment(request.TenantCode, "TenantCode");
        var objectType = ValidateSegment(request.ObjectType, "ObjectType");
        if (request.ObjectId <= 0)
        {
            throw new ArgumentException("ObjectId phải lớn hơn 0.");
        }

        var originalFileName = SanitizeFileName(request.OriginalFileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        ValidateMetadata(request, extension);

        var storageKey = string.Join(
            '/',
            tenantCode,
            objectType,
            request.ObjectId.ToString(),
            $"{Guid.NewGuid():N}{extension}");
        var destinationPath = ResolveOwnedPath(tenantCode, storageKey);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Storage path không hợp lệ.");
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = destinationPath + ".tmp";
        try
        {
            var result = await CopyValidatedAsync(
                request,
                extension,
                temporaryPath,
                cancellationToken);
            System.IO.File.Move(temporaryPath, destinationPath, overwrite: false);

            return new StoredPrivateFile(
                storageKey,
                originalFileName,
                request.ContentType.Trim(),
                result.FileSize,
                result.Sha256,
                DateTime.UtcNow,
                tenantCode);
        }
        catch
        {
            if (System.IO.File.Exists(temporaryPath))
            {
                System.IO.File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public Task<Stream> OpenReadAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveOwnedPath(ValidateSegment(tenantCode, "TenantCode"), storageKey);
        if (!System.IO.File.Exists(path))
        {
            throw new FileNotFoundException("Không tìm thấy private file.");
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string tenantCode,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveOwnedPath(ValidateSegment(tenantCode, "TenantCode"), storageKey);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private async Task<(long FileSize, string Sha256)> CopyValidatedAsync(
        PrivateFileSaveRequest request,
        string extension,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        await using var output = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = new byte[81920];
        var signatureBuffer = new byte[16];
        var signatureLength = 0;
        long total = 0;

        while (true)
        {
            var read = await request.Content.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > request.UploadPolicy.MaximumSizeBytes)
            {
                throw new ArgumentException("Tệp vượt quá dung lượng cho phép.");
            }

            if (signatureLength < signatureBuffer.Length)
            {
                var copyLength = Math.Min(read, signatureBuffer.Length - signatureLength);
                buffer.AsSpan(0, copyLength)
                    .CopyTo(signatureBuffer.AsSpan(signatureLength));
                signatureLength += copyLength;
            }

            sha256.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (total == 0)
        {
            throw new ArgumentException("Tệp không được rỗng.");
        }

        ValidateSignature(
            extension,
            signatureBuffer.AsSpan(0, signatureLength),
            request.UploadPolicy);

        return (total, Convert.ToHexString(sha256.GetHashAndReset()).ToLowerInvariant());
    }

    private static void ValidateMetadata(
        PrivateFileSaveRequest request,
        string extension)
    {
        if (request.DeclaredSize <= 0)
        {
            throw new ArgumentException("Tệp không được rỗng.");
        }

        if (request.UploadPolicy.MaximumSizeBytes <= 0
            || request.DeclaredSize > request.UploadPolicy.MaximumSizeBytes)
        {
            throw new ArgumentException("Tệp vượt quá dung lượng cho phép.");
        }

        if (!request.UploadPolicy.AllowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Phần mở rộng tệp không được phép.");
        }

        if (!request.UploadPolicy.AllowedContentTypes.Contains(
                request.ContentType.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Content-Type của tệp không được phép.");
        }
    }

    private static void ValidateSignature(
        string extension,
        ReadOnlySpan<byte> prefix,
        PrivateFileUploadPolicy policy)
    {
        if (!policy.AllowedSignatures.TryGetValue(extension, out var signatures)
            || signatures.Count == 0)
        {
            return;
        }

        foreach (var signature in signatures)
        {
            if (prefix.StartsWith(signature))
            {
                return;
            }
        }

        throw new ArgumentException("Chữ ký tệp không khớp với định dạng khai báo.");
    }

    private string ResolveOwnedPath(string tenantCode, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || Path.IsPathRooted(storageKey)
            || storageKey.Contains('\\'))
        {
            throw new ArgumentException("StorageKey không hợp lệ.");
        }

        var segments = storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4
            || !string.Equals(segments[0], tenantCode, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "Tệp không thuộc tenant hiện tại.");
        }

        foreach (var segment in segments.Take(segments.Length - 1))
        {
            ValidateSegment(segment, "StorageKey");
        }

        var path = Path.GetFullPath(
            Path.Combine(_rootPath, Path.Combine(segments)));
        if (!IsWithinRoot(path, _rootPath))
        {
            throw new UnauthorizedAccessException("StorageKey nằm ngoài private storage.");
        }

        return path;
    }

    private static bool IsWithinRoot(string path, string root)
    {
        if (string.Equals(
                path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalizedRoot = root.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string ValidateSegment(string value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || !SafeSegmentRegex().IsMatch(normalized))
        {
            throw new ArgumentException($"{fieldName} không hợp lệ.");
        }

        return normalized;
    }

    private static string SanitizeFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName?.Trim());
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized is "." or ".."
            || normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Tên tệp không hợp lệ.");
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSegmentRegex();
}
