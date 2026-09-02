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

    [Theory]
    [InlineData("signed.pdf", "application/pdf", "255044462D74657374")]
    [InlineData("signed.jpg", "image/jpeg", "FFD8FF001122")]
    [InlineData("signed.jpeg", "image/jpeg", "FFD8FF334455")]
    [InlineData("signed.png", "image/png", "89504E470D0A1A0A1122")]
    public async Task ContractEvidence_AllAllowedFormatsPassMetadataAndSignatureValidation(
        string fileName,
        string contentType,
        string contentHex)
    {
        var storage = CreateStorage();
        var content = Convert.FromHexString(contentHex);

        var stored = await storage.SaveAsync(new PrivateFileSaveRequest(
            new MemoryStream(content),
            fileName,
            contentType,
            content.Length,
            "tenant-a",
            "contract-evidence",
            12,
            PrivateFileUploadPolicies.ContractEvidence()));

        Assert.Equal(fileName, stored.OriginalFileName);
        Assert.Equal(contentType, stored.ContentType);
        Assert.Equal(content.Length, stored.FileSize);
        Assert.Equal(64, stored.Sha256.Length);
    }

    [Theory]
    [InlineData("signed.exe", "application/pdf", "255044462D74657374")]
    [InlineData("signed.pdf", "text/plain", "255044462D74657374")]
    [InlineData("signed.pdf", "application/pdf", "6E6F742D612D706466")]
    public async Task ContractEvidence_RejectsInvalidExtensionContentTypeOrSignature(
        string fileName,
        string contentType,
        string contentHex)
    {
        var storage = CreateStorage();
        var content = Convert.FromHexString(contentHex);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(new PrivateFileSaveRequest(
                new MemoryStream(content),
                fileName,
                contentType,
                content.Length,
                "tenant-a",
                "contract-evidence",
                12,
                PrivateFileUploadPolicies.ContractEvidence())));
    }

    [Fact]
    public async Task ContractEvidence_RejectsDeclaredFileLargerThanPolicyLimit()
    {
        var storage = CreateStorage();
        var content = "%PDF-test"u8.ToArray();
        var policy = PrivateFileUploadPolicies.ContractEvidence();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(new PrivateFileSaveRequest(
                new MemoryStream(content),
                "signed.pdf",
                "application/pdf",
                policy.MaximumSizeBytes + 1,
                "tenant-a",
                "contract-evidence",
                12,
                policy)));
    }

    [Theory]
    [InlineData("avatar.jpg", "image/jpeg", "FFD8FF001122")]
    [InlineData("cover.png", "image/png", "89504E470D0A1A0A1122")]
    public async Task ProfileImage_AllowsOnlyValidatedJpegAndPng(
        string fileName,
        string contentType,
        string contentHex)
    {
        var storage = CreateStorage();
        var content = Convert.FromHexString(contentHex);

        var stored = await storage.SaveAsync(new PrivateFileSaveRequest(
            new MemoryStream(content),
            fileName,
            contentType,
            content.Length,
            "tenant-a",
            "EmployeeProfileAvatar",
            12,
            PrivateFileUploadPolicies.ProfileImage(5 * 1024 * 1024)));

        Assert.Equal(contentType, stored.ContentType);
        Assert.Equal(64, stored.Sha256.Length);
    }

    [Theory]
    [InlineData("avatar.pdf", "application/pdf", "255044462D74657374")]
    [InlineData("avatar.png", "image/png", "FFD8FF001122")]
    [InlineData("avatar.svg", "image/svg+xml", "3C7376673E")]
    public async Task ProfileImage_RejectsUnsupportedOrSpoofedFiles(
        string fileName,
        string contentType,
        string contentHex)
    {
        var storage = CreateStorage();
        var content = Convert.FromHexString(contentHex);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(new PrivateFileSaveRequest(
                new MemoryStream(content),
                fileName,
                contentType,
                content.Length,
                "tenant-a",
                "EmployeeProfileAvatar",
                12,
                PrivateFileUploadPolicies.ProfileImage(5 * 1024 * 1024))));
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
