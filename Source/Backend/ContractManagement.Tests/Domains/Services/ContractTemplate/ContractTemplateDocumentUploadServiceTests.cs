using System.Security.Cryptography;
using System.Text.Json;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.Tests.Domains.Services.ContractTemplate;

public sealed class ContractTemplateDocumentUploadServiceTests
{
    private const int TenantId = 909;
    private const int AdminOfficerId = 901;
    private const int ManagerId = 902;
    private const int TemplateId = 903;
    private const int VersionId = 904;
    private const int InactiveAdminOfficerId = 905;

    [Fact]
    public async Task ActiveAdminOfficer_UploadsValidDocxAndStagesSafeAudit()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage);
        var rowVersion = await GetVersionRowVersionAsync(context);
        var bytes = CreateDocument(RequiredTokens());

        var result = await service.UploadDocumentAsync(
            VersionId,
            Request(bytes, rowVersion),
            AdminOfficerId);

        Assert.Equal(TemplateValidationStatus.Valid, result.ValidationStatus);
        Assert.Equal(1, result.DocumentFileId);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            result.DocumentHash);
        var fields = await context.TblContractTemplateFields
            .Where(field => field.TemplateVersionId == VersionId)
            .OrderBy(field => field.DisplayOrder)
            .ToListAsync();
        Assert.Equal(RequiredTokens().Count(), fields.Count);
        Assert.All(fields, field => Assert.NotNull(
            SoftwareSupplyPlaceholderCatalog.Find(field.PlaceholderKey)));

        var audit = await context.TblContractTemplateAudits.SingleAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.DocumentUploaded,
            audit.ActionType);
        Assert.Equal(ContractTemplateAuditResults.Succeeded, audit.Result);
        Assert.Equal(TenantId, audit.TenantId);
        Assert.Equal(AdminOfficerId, audit.ActorEmployeeId);
        var auditJson = string.Concat(audit.PreviousValuesJson,
            audit.NewValuesJson, audit.FailureCode);
        Assert.DoesNotContain("template-version-904.docx", auditJson);
        Assert.DoesNotContain("CONTRACT_CODE", auditJson);
    }

    [Fact]
    public async Task CatalogInvalidDocx_ReplacesArtifactAndLocksPublish()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage);

        var first = await service.UploadDocumentAsync(
            VersionId,
            Request(CreateDocument(RequiredTokens()),
                await GetVersionRowVersionAsync(context)),
            AdminOfficerId);
        var invalidTokens = RequiredTokens()
            .Where(token => token != "{{CONTRACT_CODE}}");
        var invalid = await service.UploadDocumentAsync(
            VersionId,
            Request(CreateDocument(invalidTokens), first.RowVersion),
            AdminOfficerId);

        Assert.Equal(TemplateValidationStatus.Invalid, invalid.ValidationStatus);
        Assert.Equal(2, invalid.DocumentFileId);
        Assert.Contains("MissingRequiredPlaceholder:CONTRACT_CODE",
            invalid.ValidationMessage);
        Assert.Contains(1, storage.DeletedFileIds);
        Assert.DoesNotContain("CONTRACT_CODE", await context.TblContractTemplateFields
            .Where(field => field.TemplateVersionId == VersionId)
            .Select(field => field.PlaceholderKey)
            .ToListAsync());
        Assert.Contains(await context.TblContractTemplateAudits.ToListAsync(), audit =>
            audit.ActionType == ContractTemplateAuditActionTypes.DocumentReplaced);
        Assert.Contains(await context.TblContractTemplateAudits.ToListAsync(), audit =>
            audit.ActionType == ContractTemplateAuditActionTypes.ValidationInvalid
            && audit.Result == ContractTemplateAuditResults.Invalid);
    }

    [Fact]
    public async Task TechnicalRejection_DoesNotStoreArtifactOrChangeVersion()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage);
        var rowVersion = await GetVersionRowVersionAsync(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            VersionId,
            Request([1, 2, 3, 4], rowVersion),
            AdminOfficerId));

        var version = await context.TblContractTemplateVersions.SingleAsync();
        Assert.Null(version.DocumentFileId);
        Assert.Equal(TemplateValidationStatus.NotValidated,
            (TemplateValidationStatus)version.ValidationStatus);
        Assert.Empty(storage.UploadedFileIds);
        Assert.Empty(await context.TblFileStorages.ToListAsync());
        var audit = await context.TblContractTemplateAudits.SingleAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.ValidationRejected,
            audit.ActionType);
        Assert.Equal("NotZipPackage", audit.FailureCode);
    }

    [Fact]
    public async Task OldArtifactCleanupFailure_AfterCommitDoesNotReverseNewVersion()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var storage = new TestFileStorage(context) { FailOnDelete = true };
        var service = CreateService(context, storage);

        var first = await service.UploadDocumentAsync(
            VersionId,
            Request(CreateDocument(RequiredTokens()),
                await GetVersionRowVersionAsync(context)),
            AdminOfficerId);
        var second = await service.UploadDocumentAsync(
            VersionId,
            Request(CreateDocument(RequiredTokens()), first.RowVersion),
            AdminOfficerId);

        Assert.Equal(2, second.DocumentFileId);
        Assert.Equal(TemplateValidationStatus.Valid, second.ValidationStatus);
        Assert.Contains(1, storage.DeleteAttempts);
    }

    [Fact]
    public async Task NonDraftVersion_CannotUpload()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var version = await context.TblContractTemplateVersions.SingleAsync();
        version.Status = (byte)TemplateVersionStatus.Published;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage);
        var rowVersion = await GetVersionRowVersionAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(
                VersionId,
                Request(CreateDocument(RequiredTokens()),
                    rowVersion),
                AdminOfficerId));

        Assert.Empty(storage.UploadedFileIds);
    }

    [Fact]
    public async Task StaleRowVersion_DoesNotUploadAndRecordsConflict()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service.UploadDocumentAsync(
                VersionId,
                Request(CreateDocument(RequiredTokens()),
                    Convert.ToBase64String([9, 9, 9, 9, 9, 9, 9, 9])),
                AdminOfficerId));

        Assert.Empty(storage.UploadedFileIds);
        var audit = await context.TblContractTemplateAudits.SingleAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.ConcurrencyConflict,
            audit.ActionType);
        Assert.Equal(ContractTemplateAuditResults.Conflict, audit.Result);
    }

    [Fact]
    public async Task AuditFailure_RollsBackVersionAndFieldSnapshotAndCompensatesArtifact()
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context, includeExistingField: true);
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage,
            writerDecorator: writer => new ThrowAfterStagingWriter(writer));
        var rowVersion = await GetVersionRowVersionAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(
                VersionId,
                Request(CreateDocument(RequiredTokens()),
                    rowVersion),
                AdminOfficerId));

        var version = await context.TblContractTemplateVersions.SingleAsync();
        Assert.Null(version.DocumentFileId);
        Assert.Contains("EXISTING_FIELD", await context.TblContractTemplateFields
            .Select(field => field.PlaceholderKey)
            .ToListAsync());
        Assert.Contains(1, storage.CompensatedFileIds);
        Assert.Empty(await context.TblContractTemplateAudits.ToListAsync());
    }

    [Theory]
    [InlineData(ManagerId)]
    [InlineData(InactiveAdminOfficerId)]
    public async Task NonAdminOfficer_CannotUpload(int employeeId)
    {
        await using var context = CreateContext();
        await SeedDraftAsync(context);
        var storage = new TestFileStorage(context);
        var service = CreateService(context, storage);
        var rowVersion = await GetVersionRowVersionAsync(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UploadDocumentAsync(
                VersionId,
                Request(CreateDocument(RequiredTokens()),
                    rowVersion),
                employeeId));

        Assert.Empty(storage.UploadedFileIds);
    }

    private static ContractTemplateService CreateService(
        DbDtctechContext context,
        TestFileStorage storage,
        Func<IContractTemplateAuditWriter, IContractTemplateAuditWriter>?
            writerDecorator = null)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-909",
            "Tenant 909",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "slice-09-test"
        };
        httpContext.Request.Headers.UserAgent = "ContractManagement.Slice09.Tests";
        IContractTemplateAuditWriter writer = new ContractTemplateAuditWriter(
            context,
            tenant,
            new HttpContextAccessor { HttpContext = httpContext });
        if (writerDecorator is not null)
        {
            writer = writerDecorator(writer);
        }

        return new ContractTemplateService(
            context,
            storage,
            new ContractTemplateDocumentValidator(),
            writer);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedDraftAsync(
        DbDtctechContext context,
        bool includeExistingField = false)
    {
        var now = DateTime.UtcNow;
        context.TblEmployees.AddRange(
            new TblEmployee
            {
                EmployeeId = AdminOfficerId,
                EmployeeType = (byte)EmployeeType.AdminOfficer,
                Status = 1,
                EmployeeFullName = "Admin Officer"
            },
            new TblEmployee
            {
                EmployeeId = ManagerId,
                EmployeeType = (byte)EmployeeType.Manager,
                Status = 1,
                EmployeeFullName = "Manager"
            },
            new TblEmployee
            {
                EmployeeId = InactiveAdminOfficerId,
                EmployeeType = (byte)EmployeeType.AdminOfficer,
                Status = 0,
                EmployeeFullName = "Inactive Admin Officer"
            });
        context.TblContractTemplates.Add(new TblContractTemplate
        {
            TemplateId = TemplateId,
            TemplateCode = "SLICE09",
            TemplateName = "Slice 09",
            DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            IsActive = true,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [1, 1, 1, 1, 1, 1, 1, 1]
        });
        context.TblContractTemplateVersions.Add(new TblContractTemplateVersion
        {
            TemplateVersionId = VersionId,
            TemplateId = TemplateId,
            VersionNo = 1,
            Status = (byte)TemplateVersionStatus.Draft,
            ValidationStatus = (byte)TemplateValidationStatus.NotValidated,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [2, 2, 2, 2, 2, 2, 2, 2]
        });
        if (includeExistingField)
        {
            context.TblContractTemplateFields.Add(new TblContractTemplateField
            {
                TemplateVersionId = VersionId,
                PlaceholderKey = "EXISTING_FIELD",
                FieldLabel = "Existing",
                DataSource = "Existing.Value",
                DisplayOrder = 0,
                CreatedEmployeeId = AdminOfficerId,
                CreatedDate = now,
                RowVersion = [3, 3, 3, 3, 3, 3, 3, 3]
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task<string> GetVersionRowVersionAsync(
        DbDtctechContext context) => Convert.ToBase64String(await context
            .TblContractTemplateVersions
            .AsNoTracking()
            .Where(version => version.TemplateVersionId == VersionId)
            .Select(version => version.RowVersion)
            .SingleAsync());

    private static UploadContractTemplateDocumentRequest Request(
        byte[] bytes,
        string versionRowVersion) => new()
    {
        File = new FormFile(new MemoryStream(bytes), 0, bytes.LongLength,
            "File", "untrusted-original-name.docx"),
        VersionRowVersion = versionRowVersion
    };

    private static IEnumerable<string> RequiredTokens() =>
        SoftwareSupplyPlaceholderCatalog.GetAll()
            .Where(item => item.IsRequired)
            .Select(item => $"{{{{{item.Key}}}}}");

    private static byte[] CreateDocument(IEnumerable<string> tokens)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new W.Body();
            foreach (var token in tokens)
            {
                body.Append(new W.Paragraph(new W.Run(new W.Text(token))));
            }

            body.Append(new W.SectionProperties());
            mainPart.Document = new W.Document(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private sealed class TestFileStorage(DbDtctechContext context)
        : IFileStorageService
    {
        private int _nextFileId = 1;

        public List<int> UploadedFileIds { get; } = [];

        public List<int> DeletedFileIds { get; } = [];

        public List<int> CompensatedFileIds { get; } = [];

        public List<int> DeleteAttempts { get; } = [];

        public bool FailOnDelete { get; init; }

        public async Task<FileStorageResponse> UploadAsync(
            IFormFile file,
            string objectType,
            int objectId,
            int uploadedBy)
        {
            var fileId = _nextFileId++;
            context.TblFileStorages.Add(new TblFileStorage
            {
                FileId = fileId,
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = file.FileName,
                FilePath = $"/tests/{fileId}.docx",
                FileType = "docx",
                FileSize = file.Length,
                UploadedByUserId = uploadedBy,
                UploadedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            UploadedFileIds.Add(fileId);
            return new FileStorageResponse
            {
                FileId = fileId,
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = file.FileName,
                FilePath = $"/tests/{fileId}.docx",
                FileType = "docx",
                FileSize = file.Length,
                UploadedByUserId = uploadedBy,
                UploadedDate = DateTime.UtcNow
            };
        }

        public Task<(Stream Stream, string FileName)?> DownloadAsync(int fileId) =>
            Task.FromResult<(Stream Stream, string FileName)?>(null);

        public Task<List<FileStorageResponse>> GetByObjectAsync(
            string objectType,
            int objectId) => Task.FromResult(new List<FileStorageResponse>());

        public async Task DeleteAsync(int fileId)
        {
            DeleteAttempts.Add(fileId);
            if (FailOnDelete)
            {
                throw new IOException("Simulated old artifact cleanup failure.");
            }

            var file = await context.TblFileStorages.FindAsync(fileId);
            if (file is not null)
            {
                context.TblFileStorages.Remove(file);
                await context.SaveChangesAsync();
            }
            DeletedFileIds.Add(fileId);
        }

        public async Task DeleteUploadedArtifactAsync(FileStorageResponse file)
        {
            var metadata = await context.TblFileStorages.FindAsync(file.FileId);
            if (metadata is not null)
            {
                context.TblFileStorages.Remove(metadata);
                await context.SaveChangesAsync();
            }

            CompensatedFileIds.Add(file.FileId);
        }
    }

    private sealed class ThrowAfterStagingWriter(
        IContractTemplateAuditWriter inner) : IContractTemplateAuditWriter
    {
        public void StageAudits(
            IReadOnlyCollection<ContractTemplateAuditWriteRequest> requests)
        {
            inner.StageAudits(requests);
            throw new InvalidOperationException("Simulated template audit failure.");
        }
    }
}
