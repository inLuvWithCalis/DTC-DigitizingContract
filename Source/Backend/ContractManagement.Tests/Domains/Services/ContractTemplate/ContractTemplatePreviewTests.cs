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

public sealed class ContractTemplatePreviewTests
{
    private const int TenantId = 1010;
    private const int AdminOfficerId = 1011;
    private const int ManagerId = 1012;
    private const int InactiveAdminOfficerId = 1013;
    private const int TemplateId = 1014;
    private const int VersionId = 1015;

    [Fact]
    public void Dataset_CoversExactlyTheCurrentSoftwareSupplyCatalog()
    {
        var catalogKeys = SoftwareSupplyPlaceholderCatalog.GetAll()
            .Select(item => item.Key)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(catalogKeys.SetEquals(
            SoftwareSupplyPreviewDatasetV1.CoveredPlaceholderKeys));
        Assert.Equal("V1", SoftwareSupplyPreviewDatasetV1.Version);
        Assert.Equal(2, SoftwareSupplyPreviewDatasetV1.Items.Count);
        Assert.Equal(2, SoftwareSupplyPreviewDatasetV1.Payments.Count);
        Assert.Equal(100m, SoftwareSupplyPreviewDatasetV1.Payments.Sum(item => item.Percent));
        Assert.Equal(SoftwareSupplyPreviewDatasetV1.Items.Sum(item => item.TotalAmount),
            SoftwareSupplyPreviewDatasetV1.Payments.Sum(item => item.Amount));
    }

    [Fact]
    public void Renderer_CreatesOpenablePreview_ReplacesAllTokensAndPreservesSourceBytes()
    {
        var source = CreateSourceDocument(includeHeaderFooterAndNotes: true);
        var sourceHash = SHA256.HashData(source);
        var renderer = new ContractTemplatePreviewRenderer();

        var preview = renderer.Render(source, ContractLanguageMode.Bilingual);

        Assert.NotEqual(source, preview);
        Assert.Equal(sourceHash, SHA256.HashData(source));
        using var stream = new MemoryStream(preview);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = ReadAllText(document.MainDocumentPart!);
        Assert.DoesNotContain("{{", text);
        Assert.Contains(SoftwareSupplyPreviewDatasetV1.LegalDisclaimer, text);
        Assert.Contains("36.093.750 VND", text);
        Assert.Contains("Nguyễn Văn Mẫu", text);
        Assert.Contains("Điều 4.", text);
        Assert.True(document.MainDocumentPart!.Document!.Body!
            .Elements<W.Table>().Count() >= 2);
    }

    [Fact]
    public void Renderer_WithContractData_UsesProvidedSnapshotInsteadOfSampleDataset()
    {
        var scalarValues = SoftwareSupplyPlaceholderCatalog.GetAll()
            .Where(item => item.DataKind == TemplatePlaceholderDataKind.Scalar)
            .ToDictionary(item => item.Key, _ => string.Empty, StringComparer.Ordinal);
        scalarValues["CONTRACT_CODE"] = "HD-REAL-001";
        scalarValues["CUSTOMER_NAME"] = "Công ty Khách hàng Thật";
        scalarValues["CUSTOMER_TAX_CODE"] = "0109999999";

        var renderData = new ContractTemplateRenderData(
            scalarValues,
            [new ContractTemplateRenderItem(
                1, "Sản phẩm", "Phần mềm thật", 2, 1_000_000m,
                "10%", "8%", 1_944_000m)],
            [],
            [new ContractTemplateRenderTerm(
                1, "Phạm vi thật", "Actual scope", "Nội dung thật", "Actual content")],
            new ContractTemplateRenderSignature("ĐẠI DIỆN BÊN CUNG CẤP", "Nhân viên Thật"),
            new ContractTemplateRenderSignature("ĐẠI DIỆN BÊN KHÁCH HÀNG", "Khách hàng Thật"),
            string.Empty);

        var rendered = new ContractTemplatePreviewRenderer().Render(
            CreateSourceDocument(),
            ContractLanguageMode.Bilingual,
            renderData);

        using var stream = new MemoryStream(rendered);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = ReadAllText(document.MainDocumentPart!);
        Assert.Contains("HD-REAL-001", text);
        Assert.Contains("Công ty Khách hàng Thật", text);
        Assert.Contains("Phần mềm thật", text);
        Assert.Contains("Nhân viên Thật", text);
        Assert.DoesNotContain("Nguyễn Minh An", text);
        Assert.DoesNotContain("CUS-DEMO-2026", text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Renderer_RejectsDynamicBlockOutsideStandaloneBodyParagraph(bool inHeader)
    {
        var renderer = new ContractTemplatePreviewRenderer();
        var source = CreateSourceDocument(
            includeHeaderFooterAndNotes: false,
            dynamicTermsInHeader: inHeader,
            dynamicTermsMixedWithText: !inHeader);

        var exception = Assert.Throws<ContractTemplatePreviewException>(() =>
            renderer.Render(source, ContractLanguageMode.Vietnamese));

        Assert.Equal("PreviewLayoutUnsupported", exception.FailureCode);
    }

    [Fact]
    public async Task ActiveAdminOfficer_GeneratesCurrentPreview_ReusesItAndAuditsOutput()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var renderer = new CountingPreviewRenderer();
        var service = CreateService(context, storage, renderer: renderer);
        var originalVersion = await GetVersionAsync(context);

        var generated = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest(originalVersion.RowVersion),
            AdminOfficerId);
        var reused = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest(generated.RowVersion),
            AdminOfficerId);

        Assert.Equal(2, generated.PreviewFileId);
        Assert.True(generated.IsCurrent);
        Assert.False(generated.IsReused);
        Assert.False(string.Equals(Convert.ToBase64String(originalVersion.RowVersion), generated.RowVersion,
            StringComparison.Ordinal));
        Assert.Equal(generated.PreviewFileId, reused.PreviewFileId);
        Assert.True(reused.IsReused);
        Assert.Single(storage.UploadedFileIds);
        Assert.Equal(1, renderer.RenderCalls);

        var audit = await context.TblContractTemplateAudits.SingleAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.PreviewGenerated,
            audit.ActionType);
        Assert.Equal(ContractTemplateAuditResults.Succeeded, audit.Result);
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            audit.NewValuesJson!);
        Assert.Equal(generated.PreviewFileId,
            values!["PreviewFileId"].GetInt32());
        Assert.True(values["PreviewSizeBytes"].GetInt64() > 0);
    }

    [Fact]
    public async Task DownloadPreview_EnforcesVersionObjectAuthorizationAndNeverReturnsStaleArtifact()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage);
        var generated = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest((await GetVersionAsync(context)).RowVersion),
            AdminOfficerId);

        var download = await service.DownloadPreviewAsync(
            VersionId,
            AdminOfficerId);
        await using (download.Stream)
        using (var preview = WordprocessingDocument.Open(download.Stream, false))
        {
            Assert.NotNull(preview.MainDocumentPart);
        }

        var version = await context.TblContractTemplateVersions.SingleAsync();
        version.PreviewSourceHash = new string('f', 64);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stale = await Assert.ThrowsAsync<ContractTemplatePreviewException>(() =>
            service.DownloadPreviewAsync(VersionId, AdminOfficerId));
        Assert.Equal("PreviewStale", stale.FailureCode);
        Assert.Contains(generated.PreviewFileId, storage.StoredFileIds);
    }

    [Fact]
    public async Task ChangedCatalogOrDatasetFingerprint_MakesPreviewStaleAndReplacesItOnNextRender()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage);
        var first = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest((await GetVersionAsync(context)).RowVersion),
            AdminOfficerId);

        // This stored hash represents a preview emitted by an earlier catalog
        // or dataset release. The current V1 fingerprint must not reuse it.
        var version = await context.TblContractTemplateVersions.SingleAsync();
        version.PreviewSourceHash = new string('e', 64);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stale = await Assert.ThrowsAsync<ContractTemplatePreviewException>(() =>
            service.DownloadPreviewAsync(VersionId, AdminOfficerId));
        Assert.Equal("PreviewStale", stale.FailureCode);

        var replacement = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest(first.RowVersion),
            AdminOfficerId);
        Assert.NotEqual(first.PreviewFileId, replacement.PreviewFileId);
        Assert.Contains(first.PreviewFileId, storage.DeletedFileIds);
    }

    [Fact]
    public async Task NewDocument_MakesPreviewStale_CleansOldArtifactAfterCommit_AndAllowsNewPreview()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage);
        var first = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest((await GetVersionAsync(context)).RowVersion),
            AdminOfficerId);
        var replacementBytes = CreateSourceDocument(
            additionalBodyText: "Replacement DOCX source for stale preview test.");

        var uploaded = await service.UploadDocumentAsync(
            VersionId,
            new UploadContractTemplateDocumentRequest
            {
                File = new FormFile(new MemoryStream(replacementBytes), 0,
                    replacementBytes.LongLength,
                    "File", "replacement.docx"),
                VersionRowVersion = first.RowVersion
            },
            AdminOfficerId);

        var stale = await Assert.ThrowsAsync<ContractTemplatePreviewException>(() =>
            service.DownloadPreviewAsync(VersionId, AdminOfficerId));
        Assert.Equal("PreviewStale", stale.FailureCode);
        Assert.Contains(first.PreviewFileId, storage.DeletedFileIds);

        var replacement = await service.GeneratePreviewAsync(
            VersionId,
            PreviewRequest(uploaded.RowVersion),
            AdminOfficerId);
        Assert.NotEqual(first.PreviewFileId, replacement.PreviewFileId);
        Assert.True(replacement.IsCurrent);
    }

    [Fact]
    public async Task IncorrectDynamicLayout_IsRejectedWithAuditWithoutArtifactOrPreviewMetadata()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument(
            dynamicTermsMixedWithText: true));
        var service = CreateService(context, storage);
        var rowVersion = (await GetVersionAsync(context)).RowVersion;

        var exception = await Assert.ThrowsAsync<ContractTemplatePreviewException>(() =>
            service.GeneratePreviewAsync(
                VersionId,
                PreviewRequest(rowVersion),
                AdminOfficerId));

        Assert.Equal("PreviewLayoutUnsupported", exception.FailureCode);
        Assert.Empty(storage.UploadedFileIds);
        var version = await GetVersionAsync(context);
        Assert.Null(version.PreviewFileId);
        var audit = await context.TblContractTemplateAudits.SingleAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.PreviewRejected,
            audit.ActionType);
        Assert.Equal("PreviewLayoutUnsupported", audit.FailureCode);
    }

    [Theory]
    [InlineData(ManagerId)]
    [InlineData(InactiveAdminOfficerId)]
    public async Task NonAdminOrInactiveActor_CannotGeneratePreview(int employeeId)
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage);
        var rowVersion = (await GetVersionAsync(context)).RowVersion;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GeneratePreviewAsync(
                VersionId,
                PreviewRequest(rowVersion),
                employeeId));
        Assert.Empty(storage.UploadedFileIds);
    }

    [Fact]
    public async Task CrossTenantVersion_IsNotDiscoverableFromAnotherTenantContext()
    {
        await using var owner = CreateContext();
        var ownerStorage = new TestFileStorage(owner);
        await SeedValidDraftAsync(owner, ownerStorage, CreateSourceDocument());

        await using var otherTenant = CreateContext();
        var otherStorage = new TestFileStorage(otherTenant);
        await SeedEmployeeAsync(otherTenant, AdminOfficerId,
            EmployeeType.AdminOfficer, active: true);
        await otherTenant.SaveChangesAsync();
        var service = CreateService(otherTenant, otherStorage, tenantId: TenantId + 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GeneratePreviewAsync(
                VersionId,
                PreviewRequest(Convert.ToBase64String([2, 2, 2, 2, 2, 2, 2, 2])),
                AdminOfficerId));
    }

    [Theory]
    [InlineData(TemplateVersionStatus.Published, TemplateValidationStatus.Valid, true)]
    [InlineData(TemplateVersionStatus.Draft, TemplateValidationStatus.NotValidated, true)]
    [InlineData(TemplateVersionStatus.Draft, TemplateValidationStatus.Valid, false)]
    public async Task PreviewPrerequisites_BlockNonDraftUnvalidatedOrMissingSource(
        TemplateVersionStatus status,
        TemplateValidationStatus validationStatus,
        bool hasDocument)
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var version = await context.TblContractTemplateVersions.SingleAsync();
        version.Status = (byte)status;
        version.ValidationStatus = (byte)validationStatus;
        if (validationStatus == TemplateValidationStatus.NotValidated)
        {
            version.ValidatedByEmployeeId = null;
            version.ValidatedDate = null;
        }
        if (!hasDocument)
        {
            version.DocumentFileId = null;
            version.DocumentHash = null;
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateService(context, storage);
        var rowVersion = (await GetVersionAsync(context)).RowVersion;

        var exception = await Assert.ThrowsAsync<ContractTemplatePreviewException>(() =>
            service.GeneratePreviewAsync(
                VersionId,
                PreviewRequest(rowVersion),
                AdminOfficerId));
        Assert.Equal("PreviewPrerequisiteNotMet", exception.FailureCode);
        Assert.Empty(storage.UploadedFileIds);
    }

    [Fact]
    public async Task StaleRowVersion_IsBlockedAndRecordsPreviewConcurrencyAudit()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service.GeneratePreviewAsync(
                VersionId,
                PreviewRequest(Convert.ToBase64String([9, 9, 9, 9, 9, 9, 9, 9])),
                AdminOfficerId));
        Assert.Empty(storage.UploadedFileIds);
        var audit = await context.TblContractTemplateAudits.SingleAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.PreviewConcurrencyConflict,
            audit.ActionType);
    }

    [Fact]
    public async Task AuditFailure_CompensatesNewPreviewAndLeavesVersionWithoutPreview()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage,
            writerDecorator: writer => new ThrowAfterStagingWriter(writer));
        var rowVersion = (await GetVersionAsync(context)).RowVersion;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GeneratePreviewAsync(
                VersionId,
                PreviewRequest(rowVersion),
                AdminOfficerId));

        var version = await GetVersionAsync(context);
        Assert.Null(version.PreviewFileId);
        Assert.Contains(2, storage.CompensatedFileIds);
    }

    [Fact]
    public async Task Publish_CreatesImmutablePdf_ThenRetireKeepsBothPreviewArtifacts()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var pdfRenderer = new FakePdfRenderer("%PDF-1.7\npreview");
        var service = CreateService(context, storage, pdfRenderer: pdfRenderer);

        var preview = await service.GeneratePreviewAsync(VersionId,
            PreviewRequest((await GetVersionAsync(context)).RowVersion),
            AdminOfficerId);
        var published = await service.PublishAsync(VersionId,
            PublishRequest(preview.RowVersion), AdminOfficerId);

        Assert.Equal(TemplateVersionStatus.Published, published.Status);
        Assert.Equal(3, published.PublishedPreviewPdfFileId);
        Assert.Equal(1, pdfRenderer.Calls);
        Assert.Equal(VersionId, (await context.TblContractTemplates.SingleAsync())
            .CurrentPublishedVersionId);
        var pdf = await service.DownloadPublishedPreviewPdfAsync(VersionId,
            AdminOfficerId);
        await using (pdf.Stream)
        {
            Assert.StartsWith("%PDF-", await new StreamReader(pdf.Stream)
                .ReadToEndAsync());
        }

        var retired = await service.RetireAsync(VersionId,
            RetireRequest(published.RowVersion), AdminOfficerId);
        Assert.Equal(TemplateVersionStatus.Retired, retired.Status);
        Assert.Equal(3, retired.PublishedPreviewPdfFileId);
        Assert.Null((await context.TblContractTemplates.SingleAsync())
            .CurrentPublishedVersionId);
        var retiredDocx = await service.DownloadPreviewAsync(VersionId,
            AdminOfficerId);
        await using (retiredDocx.Stream)
        using (var document = WordprocessingDocument.Open(retiredDocx.Stream, false))
        {
            Assert.NotNull(document.MainDocumentPart);
        }
        Assert.Contains(preview.PreviewFileId, storage.StoredFileIds);
        Assert.Contains(retired.PublishedPreviewPdfFileId!.Value, storage.StoredFileIds);
    }

    [Fact]
    public async Task Publish_RenderFailureLeavesDraftAndPointerUnchanged_AndAuditsFailure()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        await SeedValidDraftAsync(context, storage, CreateSourceDocument());
        var service = CreateService(context, storage,
            pdfRenderer: new FailingPdfRenderer("PdfRenderTimeout"));
        var preview = await service.GeneratePreviewAsync(VersionId,
            PreviewRequest((await GetVersionAsync(context)).RowVersion),
            AdminOfficerId);

        var exception = await Assert.ThrowsAsync<ContractTemplatePdfRenderingException>(
            () => service.PublishAsync(VersionId, PublishRequest(preview.RowVersion),
                AdminOfficerId));

        Assert.Equal("PdfRenderTimeout", exception.FailureCode);
        var version = await GetVersionAsync(context);
        Assert.Equal(TemplateVersionStatus.Draft, (TemplateVersionStatus)version.Status);
        Assert.Null(version.PublishedPreviewPdfFileId);
        Assert.Null((await context.TblContractTemplates.SingleAsync())
            .CurrentPublishedVersionId);
        Assert.DoesNotContain(3, storage.UploadedFileIds);
        var audit = await context.TblContractTemplateAudits
            .OrderByDescending(item => item.ContractTemplateAuditId).FirstAsync();
        Assert.Equal(ContractTemplateAuditActionTypes.PdfRenderFailed,
            audit.ActionType);
        Assert.Equal("PdfRenderTimeout", audit.FailureCode);
    }

    [Fact]
    public async Task PublishReplacement_AutoRetiresCurrentAndMovesContractSelectionPointer()
    {
        await using var context = CreateContext();
        var storage = new TestFileStorage(context);
        var source = CreateSourceDocument();
        await SeedValidDraftAsync(context, storage, source);
        var service = CreateService(context, storage,
            pdfRenderer: new FakePdfRenderer("%PDF-1.7\npreview"));
        var firstPreview = await service.GeneratePreviewAsync(VersionId,
            PreviewRequest((await GetVersionAsync(context)).RowVersion),
            AdminOfficerId);
        var first = await service.PublishAsync(VersionId,
            PublishRequest(firstPreview.RowVersion), AdminOfficerId);

        const int nextVersionId = VersionId + 1;
        var secondSource = CreateSourceDocument(
            additionalBodyText: "Second published template version.");
        var sourceFile = await storage.UploadAsync(new FormFile(
            new MemoryStream(secondSource), 0, secondSource.LongLength,
            "File", "second-source.docx"), "ContractTemplateVersion",
            nextVersionId, AdminOfficerId);
        var now = DateTime.UtcNow;
        context.TblContractTemplateVersions.Add(new TblContractTemplateVersion
        {
            TemplateVersionId = nextVersionId,
            TemplateId = TemplateId,
            VersionNo = 2,
            Status = (byte)TemplateVersionStatus.Draft,
            ValidationStatus = (byte)TemplateValidationStatus.Valid,
            DocumentFileId = sourceFile.FileId,
            DocumentHash = Convert.ToHexString(SHA256.HashData(secondSource))
                .ToLowerInvariant(),
            ValidatedByEmployeeId = AdminOfficerId,
            ValidatedDate = now,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [3, 3, 3, 3, 3, 3, 3, 3]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var nextDraft = await context.TblContractTemplateVersions.AsNoTracking()
            .SingleAsync(version => version.TemplateVersionId == nextVersionId);
        var secondPreview = await service.GeneratePreviewAsync(nextVersionId,
            PreviewRequest(nextDraft.RowVersion), AdminOfficerId);
        var second = await service.PublishAsync(nextVersionId,
            PublishRequest(secondPreview.RowVersion), AdminOfficerId);

        var oldVersion = await context.TblContractTemplateVersions.AsNoTracking()
            .SingleAsync(version => version.TemplateVersionId == VersionId);
        Assert.Equal(TemplateVersionStatus.Retired,
            (TemplateVersionStatus)oldVersion.Status);
        Assert.Equal(TemplateVersionStatus.Published, second.Status);
        Assert.Equal(nextVersionId, (await context.TblContractTemplates.SingleAsync())
            .CurrentPublishedVersionId);
        Assert.Contains(first.PublishedPreviewPdfFileId!.Value, storage.StoredFileIds);
    }

    private static ContractTemplateService CreateService(
        DbDtctechContext context,
        TestFileStorage storage,
        int tenantId = TenantId,
        Func<IContractTemplateAuditWriter, IContractTemplateAuditWriter>?
            writerDecorator = null,
        IContractTemplatePreviewRenderer? renderer = null,
        IContractTemplatePdfRenderer? pdfRenderer = null)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            tenantId,
            $"TENANT-{tenantId}",
            $"Tenant {tenantId}",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var httpContext = new DefaultHttpContext { TraceIdentifier = "slice-10-test" };
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
            writer,
            renderer ?? new ContractTemplatePreviewRenderer(),
            pdfRenderer);
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

    private static async Task SeedValidDraftAsync(
        DbDtctechContext context,
        TestFileStorage storage,
        byte[] source)
    {
        await SeedEmployeeAsync(context, AdminOfficerId,
            EmployeeType.AdminOfficer, active: true);
        await SeedEmployeeAsync(context, ManagerId, EmployeeType.Manager,
            active: true);
        await SeedEmployeeAsync(context, InactiveAdminOfficerId,
            EmployeeType.AdminOfficer, active: false);
        var now = DateTime.UtcNow;
        var documentHash = Convert.ToHexString(SHA256.HashData(source))
            .ToLowerInvariant();
        context.TblContractTemplates.Add(new TblContractTemplate
        {
            TemplateId = TemplateId,
            TemplateCode = "PREVIEW-V1",
            TemplateName = "Preview V1",
            DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
            LanguageMode = (byte)ContractLanguageMode.Bilingual,
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
            ValidationStatus = (byte)TemplateValidationStatus.Valid,
            DocumentFileId = 1,
            DocumentHash = documentHash,
            ValidatedByEmployeeId = AdminOfficerId,
            ValidatedDate = now,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [2, 2, 2, 2, 2, 2, 2, 2]
        });
        await storage.SeedAsync(1, source, "ContractTemplateVersion", VersionId,
            AdminOfficerId);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static Task SeedEmployeeAsync(
        DbDtctechContext context,
        int employeeId,
        EmployeeType type,
        bool active)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = employeeId,
            EmployeeType = (byte)type,
            Status = active ? (byte)1 : (byte)0,
            EmployeeFullName = $"Employee {employeeId}"
        });
        return Task.CompletedTask;
    }

    private static async Task<TblContractTemplateVersion> GetVersionAsync(
        DbDtctechContext context) => await context.TblContractTemplateVersions
            .AsNoTracking()
            .SingleAsync(version => version.TemplateVersionId == VersionId);

    private static GenerateContractTemplatePreviewRequest PreviewRequest(
        byte[] rowVersion) => new()
    {
        VersionRowVersion = Convert.ToBase64String(rowVersion)
    };

    private static PublishContractTemplateVersionRequest PublishRequest(
        string rowVersion) => new()
    {
        VersionRowVersion = rowVersion
    };

    private static RetireContractTemplateVersionRequest RetireRequest(
        string rowVersion) => new()
    {
        VersionRowVersion = rowVersion
    };

    private static GenerateContractTemplatePreviewRequest PreviewRequest(
        string rowVersion) => new()
    {
        VersionRowVersion = rowVersion
    };

    private static byte[] CreateSourceDocument(
        bool includeHeaderFooterAndNotes = false,
        bool dynamicTermsInHeader = false,
        bool dynamicTermsMixedWithText = false,
        string? additionalBodyText = null)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new W.Body();
            foreach (var definition in SoftwareSupplyPlaceholderCatalog.GetAll())
            {
                if (definition.DataKind == TemplatePlaceholderDataKind.DynamicBlock)
                {
                    if (definition.Key == "CONTRACT_TERMS" && dynamicTermsInHeader)
                    {
                        continue;
                    }

                    var token = $"{{{{{definition.Key}}}}}";
                    body.Append(new W.Paragraph(new W.Run(new W.Text(
                        definition.Key == "CONTRACT_TERMS" && dynamicTermsMixedWithText
                            ? $"Không hợp lệ {token}"
                            : token))));
                }
                else
                {
                    var token = $"{{{{{definition.Key}}}}}";
                    // Split one scalar token across two runs to prove run-safe replacement.
                    body.Append(definition.Key == "CONTRACT_CODE"
                        ? new W.Paragraph(
                            new W.Run(new W.Text("Mã: {{CONTRACT_")),
                            new W.Run(new W.Text("CODE}}")))
                        : new W.Paragraph(new W.Run(new W.Text(token))));
                }
            }

            if (!string.IsNullOrWhiteSpace(additionalBodyText))
            {
                body.Append(new W.Paragraph(new W.Run(
                    new W.Text(additionalBodyText))));
            }

            var section = new W.SectionProperties();
            if (includeHeaderFooterAndNotes || dynamicTermsInHeader)
            {
                var header = mainPart.AddNewPart<HeaderPart>();
                header.Header = new W.Header(new W.Paragraph(new W.Run(
                    new W.Text(dynamicTermsInHeader
                        ? "{{CONTRACT_TERMS}}"
                        : "{{CUSTOMER_EMAIL}}"))));
                header.Header.Save();
                section.Append(new W.HeaderReference
                {
                    Type = W.HeaderFooterValues.Default,
                    Id = mainPart.GetIdOfPart(header)
                });
            }

            if (includeHeaderFooterAndNotes)
            {
                var footer = mainPart.AddNewPart<FooterPart>();
                footer.Footer = new W.Footer(new W.Paragraph(new W.Run(
                    new W.Text("{{CUSTOMER_PHONE}}"))));
                footer.Footer.Save();
                section.Append(new W.FooterReference
                {
                    Type = W.HeaderFooterValues.Default,
                    Id = mainPart.GetIdOfPart(footer)
                });

                var footnote = new W.Footnote { Id = 1 };
                footnote.Append(new W.Paragraph(
                    new W.Run(new W.Text("{{CUSTOMER_CITY}}"))));
                var footnotes = mainPart.AddNewPart<FootnotesPart>();
                footnotes.Footnotes = new W.Footnotes(footnote);
                footnotes.Footnotes.Save();

                var endnote = new W.Endnote { Id = 1 };
                endnote.Append(new W.Paragraph(
                    new W.Run(new W.Text("{{CUSTOMER_COUNTRY}}"))));
                var endnotes = mainPart.AddNewPart<EndnotesPart>();
                endnotes.Endnotes = new W.Endnotes(endnote);
                endnotes.Endnotes.Save();
            }

            body.Append(section);
            mainPart.Document = new W.Document(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static string ReadAllText(MainDocumentPart mainPart)
    {
        var roots = new List<OpenXmlPartRootElement> { mainPart.Document! };
        roots.AddRange(mainPart.HeaderParts
            .Where(part => part.Header is not null)
            .Select(part => (OpenXmlPartRootElement)part.Header!));
        roots.AddRange(mainPart.FooterParts
            .Where(part => part.Footer is not null)
            .Select(part => (OpenXmlPartRootElement)part.Footer!));
        if (mainPart.FootnotesPart?.Footnotes is not null)
        {
            roots.Add(mainPart.FootnotesPart.Footnotes);
        }
        if (mainPart.EndnotesPart?.Endnotes is not null)
        {
            roots.Add(mainPart.EndnotesPart.Endnotes);
        }

        return string.Concat(roots.SelectMany(root => root.Descendants<W.Text>())
            .Select(text => text.Text));
    }

    private sealed class TestFileStorage(DbDtctechContext context)
        : IFileStorageService
    {
        private readonly Dictionary<int, byte[]> _files = [];
        private int _nextFileId = 2;

        public List<int> UploadedFileIds { get; } = [];

        public List<int> DeletedFileIds { get; } = [];

        public List<int> CompensatedFileIds { get; } = [];

        public IReadOnlyCollection<int> StoredFileIds => _files.Keys;

        public async Task SeedAsync(
            int fileId,
            byte[] bytes,
            string objectType,
            int objectId,
            int employeeId)
        {
            _files[fileId] = bytes;
            context.TblFileStorages.Add(new TblFileStorage
            {
                FileId = fileId,
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = $"seed-{fileId}.docx",
                FilePath = $"/tests/{fileId}.docx",
                FileType = "docx",
                FileSize = bytes.LongLength,
                UploadedByUserId = employeeId,
                UploadedDate = DateTime.UtcNow
            });
            await Task.CompletedTask;
        }

        public async Task<FileStorageResponse> UploadAsync(
            IFormFile file,
            string objectType,
            int objectId,
            int uploadedBy)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var fileId = _nextFileId++;
            var bytes = stream.ToArray();
            _files[fileId] = bytes;
            context.TblFileStorages.Add(new TblFileStorage
            {
                FileId = fileId,
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = file.FileName,
                FilePath = $"/tests/{fileId}.docx",
                FileType = "docx",
                FileSize = bytes.LongLength,
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
                FileSize = bytes.LongLength,
                UploadedByUserId = uploadedBy,
                UploadedDate = DateTime.UtcNow
            };
        }

        public Task<(Stream Stream, string FileName)?> DownloadAsync(int fileId)
        {
            if (!_files.TryGetValue(fileId, out var bytes))
            {
                return Task.FromResult<(Stream Stream, string FileName)?>(null);
            }

            return Task.FromResult<(Stream Stream, string FileName)?>(
                (new MemoryStream(bytes, writable: false), $"test-{fileId}.docx"));
        }

        public Task<List<FileStorageResponse>> GetByObjectAsync(
            string objectType,
            int objectId) => Task.FromResult(new List<FileStorageResponse>());

        public async Task DeleteAsync(int fileId)
        {
            _files.Remove(fileId);
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
            _files.Remove(file.FileId);
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
            throw new InvalidOperationException("Simulated preview audit failure.");
        }
    }

    private sealed class CountingPreviewRenderer : IContractTemplatePreviewRenderer
    {
        private readonly ContractTemplatePreviewRenderer _inner = new();

        public int RenderCalls { get; private set; }

        public byte[] Render(byte[] sourceDocumentBytes,
            ContractLanguageMode languageMode)
        {
            RenderCalls++;
            return _inner.Render(sourceDocumentBytes, languageMode);
        }
    }

    private sealed class FakePdfRenderer(string content) : IContractTemplatePdfRenderer
    {
        public int Calls { get; private set; }

        public Task<byte[]> ConvertPreviewToPdfAsync(byte[] previewDocx,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(System.Text.Encoding.ASCII.GetBytes(content));
        }
    }

    private sealed class FailingPdfRenderer(string failureCode)
        : IContractTemplatePdfRenderer
    {
        public Task<byte[]> ConvertPreviewToPdfAsync(byte[] previewDocx,
            CancellationToken cancellationToken = default) =>
            throw new ContractTemplatePdfRenderingException(failureCode,
                "Simulated PDF conversion failure.");
    }
}
