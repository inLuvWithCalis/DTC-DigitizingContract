using System.Net;
using System.Security.Cryptography;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class ContractSigningServicePhase9Tests
{
    private const int TenantId = 901;
    private const int OwnerId = 902;
    private const int CustomerId = 903;
    private const int ContractId = 904;
    private const int VersionId = 905;
    private static readonly byte[] InitialRowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Upload_PersistsEvidenceAndMovesContractToSigned()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.PendingSignature);
        var storage = new TrackingPrivateStorage();
        var service = CreateService(context, storage);

        var response = await service.UploadAsync(
            ContractId,
            CreateUploadRequest("signed.pdf", "application/pdf"),
            OwnerId);

        var contract = await context.TblContracts.AsNoTracking().SingleAsync();
        var evidence = await context.TblContractSignedEvidences
            .AsNoTracking().SingleAsync();
        var file = await context.TblFileStorages.AsNoTracking()
            .SingleAsync(candidate => candidate.FileId == evidence.FileId);
        var audit = await context.TblContractAudits.AsNoTracking()
            .SingleAsync(candidate => candidate.ActionType ==
                "SignedEvidenceUploaded");

        Assert.Equal((byte)ContractStatus.Signed, contract.Status);
        Assert.Equal(SignedEvidenceStatus.Active, response.Status);
        Assert.Equal((byte)SignedEvidenceStatus.Active, evidence.Status);
        Assert.Equal("ContractSignedEvidence", file.ObjectType);
        Assert.Equal(ContractId, file.ObjectId);
        Assert.Equal("SignedEvidence", audit.SubjectType);
        Assert.Equal(evidence.SignedEvidenceId, audit.SubjectId);
        Assert.Single(storage.SavedKeys);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task Supersede_AppendsNewEvidenceAndKeepsOldFile()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Signed,
            includeActiveEvidence: true);
        var storage = new TrackingPrivateStorage();
        var service = CreateService(context, storage);

        var request = new SupersedeContractSignedEvidenceRequest
        {
            File = CreateFile("replacement.png", "image/png"),
            CurrentVersionId = VersionId,
            ContractRowVersion = Encode(InitialRowVersion),
            VersionRowVersion = Encode(InitialRowVersion),
            EvidenceRowVersion = Encode(InitialRowVersion),
            ProviderSignerName = "Provider Signer 2",
            ProviderSignerTitle = "Director",
            ProviderSigningDate = new DateTime(2026, 8, 20),
            CustomerSignerName = "Customer Signer 2",
            CustomerSignerTitle = "CEO",
            CustomerSigningDate = new DateTime(2026, 8, 21),
            Reason = "Bản trước bị thiếu một trang."
        };

        var response = await service.SupersedeAsync(
            ContractId,
            signedEvidenceId: 920,
            request,
            OwnerId);

        var evidence = await context.TblContractSignedEvidences
            .AsNoTracking()
            .OrderBy(candidate => candidate.SignedEvidenceId)
            .ToListAsync();
        var oldEvidence = evidence.Single(candidate =>
            candidate.SignedEvidenceId == 920);
        var activeEvidence = evidence.Single(candidate =>
            candidate.Status == (byte)SignedEvidenceStatus.Active);

        Assert.Equal(2, evidence.Count);
        Assert.Equal(
            (byte)SignedEvidenceStatus.Superseded,
            oldEvidence.Status);
        Assert.Equal("Bản trước bị thiếu một trang.", oldEvidence.SupersedeReason);
        Assert.Equal(oldEvidence.SignedEvidenceId, activeEvidence.SupersedesEvidenceId);
        Assert.Equal(activeEvidence.SignedEvidenceId, response.SignedEvidenceId);
        Assert.Equal(4, await context.TblFileStorages.CountAsync());
        Assert.Single(storage.SavedKeys);
        Assert.Empty(storage.DeletedKeys);
        Assert.Contains(
            await context.TblContractAudits.AsNoTracking().ToListAsync(),
            audit => audit.ActionType == "SignedEvidenceSuperseded");
    }

    [Fact]
    public async Task Supersede_CompletedContract_IsRejectedBeforeStorage()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Completed,
            includeActiveEvidence: true);
        var storage = new TrackingPrivateStorage();
        var request = new SupersedeContractSignedEvidenceRequest
        {
            File = CreateFile("replacement.pdf", "application/pdf"),
            CurrentVersionId = VersionId,
            ContractRowVersion = Encode(InitialRowVersion),
            VersionRowVersion = Encode(InitialRowVersion),
            EvidenceRowVersion = Encode(InitialRowVersion),
            ProviderSignerName = "Provider Signer",
            ProviderSignerTitle = "Director",
            ProviderSigningDate = new DateTime(2026, 8, 20),
            CustomerSignerName = "Customer Signer",
            CustomerSignerTitle = "CEO",
            CustomerSigningDate = new DateTime(2026, 8, 21),
            Reason = "Replace"
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(context, storage).SupersedeAsync(
                ContractId,
                920,
                request,
                OwnerId));

        Assert.Equal(ContractSigningErrorCodes.SigningStateChanged, exception.Code);
        Assert.Empty(storage.SavedKeys);
        Assert.Single(context.TblContractSignedEvidences);
    }

    private static ContractSigningService CreateService(
        DbDtctechContext context,
        IPrivateFileStorage storage)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-PHASE9",
            "Tenant Phase 9",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "phase-9-signing-test"
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        return new ContractSigningService(
            context,
            new ContractResourceAuthorizationService(context),
            new ContractAuditWriter(
                context,
                tenant,
                new HttpContextAccessor { HttpContext = httpContext }),
            storage,
            tenant);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), builder =>
                builder.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedAsync(
        DbDtctechContext context,
        ContractStatus status,
        bool includeActiveEvidence = false)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = OwnerId,
            EmployeeFullName = "Owner",
            EmployeeType = (byte)EmployeeType.Sale,
            Status = 1,
            RowVersion = InitialRowVersion
        });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Customer",
            Status = 1
        });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = CustomerId,
            EmployeeId = OwnerId,
            ContractType = (byte)ContractType.SoftwareSupply,
            CurrentVersionId = VersionId,
            ContractCode = "HD-PHASE9",
            ContractName = "Phase 9 Contract",
            Status = (byte)status,
            CurrencyCode = "VND",
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = VersionId,
            ContractId = ContractId,
            VersionNo = 3,
            CurrencyCode = "VND",
            IsLocked = true,
            LockedDate = DateTime.UtcNow,
            LockedByEmployeeId = OwnerId,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });
        context.TblContractApprovalRequests.Add(new TblContractApprovalRequest
        {
            ApprovalRequestId = 906,
            ContractId = ContractId,
            VersionId = VersionId,
            Status = (byte)ApprovalRequestStatus.Approved,
            SubmittedByEmployeeId = OwnerId,
            SubmittedDate = DateTime.UtcNow.AddDays(-1),
            ResolvedByEmployeeId = OwnerId,
            ResolvedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });
        context.TblFileStorages.AddRange(
            Artifact(910, "contract.docx", "docx"),
            Artifact(911, "contract.pdf", "pdf"));
        if (includeActiveEvidence)
        {
            context.TblFileStorages.Add(new TblFileStorage
            {
                FileId = 919,
                ObjectType = "ContractSignedEvidence",
                ObjectId = ContractId,
                FileName = "signed-old.pdf",
                FilePath = string.Empty,
                StorageKey = "TENANT-PHASE9/ContractSignedEvidence/904/old.pdf",
                ContentType = "application/pdf",
                Sha256 = new string('c', 64),
                TenantCode = "TENANT-PHASE9",
                FileType = "pdf",
                FileSize = 100,
                UploadedByUserId = OwnerId,
                UploadedDate = DateTime.UtcNow.AddHours(-1)
            });
            context.TblContractSignedEvidences.Add(
                new TblContractSignedEvidence
                {
                    SignedEvidenceId = 920,
                    ContractId = ContractId,
                    VersionId = VersionId,
                    FileId = 919,
                    Status = (byte)SignedEvidenceStatus.Active,
                    ProviderSignerName = "Provider Signer",
                    ProviderSignerTitle = "Director",
                    ProviderSigningDate = new DateTime(2026, 8, 18),
                    CustomerSignerName = "Customer Signer",
                    CustomerSignerTitle = "CEO",
                    CustomerSigningDate = new DateTime(2026, 8, 19),
                    UploadedByEmployeeId = OwnerId,
                    UploadedAt = DateTime.UtcNow.AddHours(-1),
                    RowVersion = InitialRowVersion
                });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static TblFileStorage Artifact(int fileId, string name, string type) =>
        new()
        {
            FileId = fileId,
            ObjectType = "ContractVersionArtifact",
            ObjectId = VersionId,
            FileName = name,
            FilePath = string.Empty,
            StorageKey = $"TENANT-PHASE9/ContractVersionArtifact/{VersionId}/{name}",
            ContentType = type == "pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Sha256 = new string(type == "pdf" ? 'a' : 'b', 64),
            TenantCode = "TENANT-PHASE9",
            FileType = type,
            FileSize = 100,
            UploadedByUserId = OwnerId,
            UploadedDate = DateTime.UtcNow
        };

    private static UploadContractSignedEvidenceRequest CreateUploadRequest(
        string fileName,
        string contentType) => new()
        {
            File = CreateFile(fileName, contentType),
            CurrentVersionId = VersionId,
            ContractRowVersion = Encode(InitialRowVersion),
            VersionRowVersion = Encode(InitialRowVersion),
            ProviderSignerName = "Provider Signer",
            ProviderSignerTitle = "Director",
            ProviderSigningDate = new DateTime(2026, 8, 20),
            CustomerSignerName = "Customer Signer",
            CustomerSignerTitle = "CEO",
            CustomerSigningDate = new DateTime(2026, 8, 21)
        };

    private static IFormFile CreateFile(string fileName, string contentType)
    {
        var bytes = "%PDF-phase9"u8.ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value);

    private sealed class TrackingPrivateStorage : IPrivateFileStorage
    {
        public List<string> SavedKeys { get; } = [];
        public List<string> DeletedKeys { get; } = [];

        public async Task<StoredPrivateFile> SaveAsync(
            PrivateFileSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            await using var memory = new MemoryStream();
            await request.Content.CopyToAsync(memory, cancellationToken);
            var key = $"{request.TenantCode}/{request.ObjectType}/{request.ObjectId}/{Guid.NewGuid():N}{Path.GetExtension(request.OriginalFileName)}";
            SavedKeys.Add(key);
            return new StoredPrivateFile(
                key,
                request.OriginalFileName,
                request.ContentType,
                memory.Length,
                Convert.ToHexString(SHA256.HashData(memory.ToArray()))
                    .ToLowerInvariant(),
                DateTime.UtcNow,
                request.TenantCode);
        }

        public Task<Stream> OpenReadAsync(
            string tenantCode,
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(
            string tenantCode,
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }
    }
}
