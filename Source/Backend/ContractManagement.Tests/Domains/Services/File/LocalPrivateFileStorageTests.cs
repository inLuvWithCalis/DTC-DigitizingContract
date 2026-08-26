using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.File;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ContractManagement.Tests.Domains.Services.File;

public sealed class LocalPrivateFileStorageTests : IDisposable
{
    private readonly string _temporaryRoot = Path.Combine(
        Path.GetTempPath(),
        $"contract-private-storage-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveOpenAndDelete_UsesPrivateRootAndReturnsSha256()
    {
        var storage = CreateStorage();
        var content = "%PDF-test"u8.ToArray();

        var stored = await storage.SaveAsync(new PrivateFileSaveRequest(
            new MemoryStream(content),
            "contract.pdf",
            "application/pdf",
            content.Length,
            "tenant-a",
            "contract-artifact",
            12,
            PrivateFileUploadPolicies.ContractEvidence()));

        Assert.StartsWith("tenant-a/contract-artifact/12/", stored.StorageKey);
        Assert.Equal(64, stored.Sha256.Length);
        await using (var stream = await storage.OpenReadAsync(
                         "tenant-a",
                         stored.StorageKey))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            Assert.Equal(content, buffer.ToArray());
        }

        await storage.DeleteAsync("tenant-a", stored.StorageKey);
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            storage.OpenReadAsync("tenant-a", stored.StorageKey));
    }

    [Fact]
    public async Task Save_RejectsExtensionContentTypeAndMagicByteMismatch()
    {
        var storage = CreateStorage();
        var content = "not-a-pdf"u8.ToArray();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(new PrivateFileSaveRequest(
                new MemoryStream(content),
                "contract.pdf",
                "application/pdf",
                content.Length,
                "tenant-a",
                "contract-artifact",
                12,
                PrivateFileUploadPolicies.ContractEvidence())));
    }

    [Fact]
    public async Task OpenRead_BlocksCrossTenantStorageKey()
    {
        var storage = CreateStorage();
        var content = "%PDF-test"u8.ToArray();
        var stored = await storage.SaveAsync(new PrivateFileSaveRequest(
            new MemoryStream(content),
            "contract.pdf",
            "application/pdf",
            content.Length,
            "tenant-a",
            "contract-artifact",
            12,
            PrivateFileUploadPolicies.ContractEvidence()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            storage.OpenReadAsync("tenant-b", stored.StorageKey));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }

    private LocalPrivateFileStorage CreateStorage()
    {
        var contentRoot = Path.Combine(_temporaryRoot, "api");
        Directory.CreateDirectory(contentRoot);
        return new LocalPrivateFileStorage(
            Options.Create(new PrivateFileStorageOptions
            {
                RootPath = Path.Combine(_temporaryRoot, "storage")
            }),
            new TestWebHostEnvironment(contentRoot));
    }

    private sealed class TestWebHostEnvironment(string contentRoot)
        : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ContractManagement.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
