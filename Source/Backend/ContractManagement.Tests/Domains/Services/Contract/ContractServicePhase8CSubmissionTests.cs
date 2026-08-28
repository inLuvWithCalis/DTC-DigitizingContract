using System.Net;
using System.Security.Cryptography;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
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

public sealed class ContractServicePhase8CSubmissionTests
{
    private const int TenantId = 8801;
    private const int ContractId = 8802;
    private const int VersionId = 8803;
    private const int OwnerId = 8804;
    private const int ManagerId = 8805;
    private const int CustomerId = 8806;
    private const int TemplateVersionId = 8807;
    private static readonly byte[] InitialRowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Submit_HappyPath_PersistsImmutableArtifactsAndInvalidatesAccess()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context, includeCustomerAccess: true);
        var renderer = new StubRenderer();
        var storage = new TrackingPrivateStorage();

        var response = await CreateService(context, renderer, storage)
            .SubmitForApprovalAsync(ContractId, CreateRequest(), OwnerId);

        var contract = await context.TblContracts.AsNoTracking().SingleAsync();
        var version = await context.TblContractVersions.AsNoTracking().SingleAsync();
        var artifacts = await context.TblFileStorages.AsNoTracking()
            .Where(file => file.ObjectType == "ContractVersionArtifact")
            .OrderBy(file => file.FileType)
            .ToListAsync();
        var link = await context.TblContractCustomerAccessLinks
            .AsNoTracking().SingleAsync();
        var challenge = await context.TblContractCustomerOtpChallenges
            .AsNoTracking().SingleAsync();
        var session = await context.TblContractCustomerAccessSessions
            .AsNoTracking().SingleAsync();

        Assert.Equal((byte)ContractStatus.PendingApproval, contract.Status);
        Assert.Null(contract.CurrentCustomerAccessLinkId);
        Assert.True(version.IsLocked);
        Assert.Contains("\"schemaVersion\":4", version.SnapshotJson);
        Assert.Equal(TemplateVersionId, version.TemplateVersionId);
        Assert.Equal(2, artifacts.Count);
        Assert.All(artifacts, artifact =>
        {
            Assert.Equal(VersionId, artifact.ObjectId);
            Assert.Equal("TENANT-8C", artifact.TenantCode);
            Assert.NotNull(artifact.StorageKey);
            Assert.Equal(string.Empty, artifact.FilePath);
            Assert.Equal(64, artifact.Sha256!.Length);
        });
        Assert.Equal(response.SubmittedDocxHash,
            artifacts.Single(item => item.FileType == "docx").Sha256);
        Assert.Equal(response.SubmittedPdfHash,
            artifacts.Single(item => item.FileType == "pdf").Sha256);
        Assert.NotNull(link.RevokedAt);
        Assert.NotNull(challenge.InvalidatedAt);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal(2, storage.SavedKeys.Count);
        Assert.Empty(storage.DeletedKeys);
        Assert.Contains(await context.TblContractAudits.AsNoTracking().ToListAsync(),
            audit => audit.ActionType == ContractAuditActionTypes.ApprovalSubmitted);
        Assert.Contains(await context.TblContractAudits.AsNoTracking().ToListAsync(),
            audit => audit.ActionType ==
                ContractAuditActionTypes.CustomerAccessLinkInvalidated);
        Assert.Contains(await context.TblContractAudits.AsNoTracking().ToListAsync(),
            audit => audit.ActionType == ContractAuditActionTypes.CustomerSessionRevoked);
    }

    [Fact]
    public async Task Submit_WithoutEligibleManager_DoesNotRenderOrChangeState()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context, includeManager: false);
        var renderer = new StubRenderer();
        var storage = new TrackingPrivateStorage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, renderer, storage).SubmitForApprovalAsync(
                ContractId, CreateRequest(), OwnerId));

        Assert.Contains("Manager", exception.Message);
        Assert.Equal(0, renderer.CallCount);
        Assert.Empty(storage.SavedKeys);
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_WorkflowAssignsSubmitterAsApprover_IsRejected()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context);
        context.TblApprovalWorkflows.Add(new TblApprovalWorkflow
        {
            WorkflowId = 8899,
            WorkflowName = "Self approval",
            ObjectType = "Contract",
            StepNo = 1,
            ApproverEmployeeId = OwnerId,
            IsActive = true
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var request = CreateRequest();
        request.WorkflowId = 8899;
        var renderer = new StubRenderer();
        var storage = new TrackingPrivateStorage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, renderer, storage).SubmitForApprovalAsync(
                ContractId, request, OwnerId));

        Assert.Contains("tự duyệt", exception.Message);
        Assert.Equal(0, renderer.CallCount);
        Assert.Empty(storage.SavedKeys);
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_MissingLegalProfileFromRenderer_DoesNotChangeState()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context);
        var renderer = new ThrowingRenderer(
            new InvalidOperationException(
                "Hồ sơ pháp lý doanh nghiệp chưa được cấu hình."));
        var storage = new TrackingPrivateStorage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, renderer, storage).SubmitForApprovalAsync(
                ContractId, CreateRequest(), OwnerId));

        Assert.Contains("Hồ sơ pháp lý", exception.Message);
        Assert.Empty(storage.SavedKeys);
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_UnsupportedContractType_StopsBeforeRenderer()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context);
        var contract = await context.TblContracts.SingleAsync();
        contract.ContractType = (byte)ContractType.SoftwareMaintenance;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var renderer = new StubRenderer();
        var storage = new TrackingPrivateStorage();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, renderer, storage).SubmitForApprovalAsync(
                ContractId, CreateRequest(), OwnerId));

        Assert.Equal(0, renderer.CallCount);
        Assert.Empty(storage.SavedKeys);
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_PdfGenerationFailure_DoesNotPersistOrLock()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context);
        var renderer = new ThrowingRenderer(
            new InvalidOperationException("PDF generation failed."));
        var storage = new TrackingPrivateStorage();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, renderer, storage).SubmitForApprovalAsync(
                ContractId, CreateRequest(), OwnerId));

        Assert.Empty(storage.SavedKeys);
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_SecondArtifactStorageFailure_DeletesFirstArtifact()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context);
        var storage = new TrackingPrivateStorage(failOnSaveNumber: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, new StubRenderer(), storage)
                .SubmitForApprovalAsync(ContractId, CreateRequest(), OwnerId));

        var savedKey = Assert.Single(storage.SavedKeys);
        Assert.Equal(savedKey, Assert.Single(storage.DeletedKeys));
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_DatabaseFailureAfterArtifacts_DeletesBothArtifacts()
    {
        var interceptor = new ToggleSaveFailureInterceptor();
        await using var context = CreateContext(interceptor);
        await SeedReadyContractAsync(context);
        interceptor.ShouldFail = true;
        var storage = new TrackingPrivateStorage();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, new StubRenderer(), storage)
                .SubmitForApprovalAsync(ContractId, CreateRequest(), OwnerId));

        Assert.Equal(2, storage.SavedKeys.Count);
        Assert.Equal(
            storage.SavedKeys.OrderBy(value => value),
            storage.DeletedKeys.OrderBy(value => value));
        interceptor.ShouldFail = false;
        await AssertSubmissionStateUnchangedAsync(context);
    }

    [Fact]
    public async Task Submit_ExistingSubmittedArtifact_IsNeverOverwritten()
    {
        await using var context = CreateContext();
        await SeedReadyContractAsync(context);
        context.TblFileStorages.Add(new TblFileStorage
        {
            ObjectType = "ContractVersionArtifact",
            ObjectId = VersionId,
            FileName = "existing.docx",
            FilePath = string.Empty,
            StorageKey = "TENANT-8C/ContractVersionArtifact/8803/existing.docx",
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Sha256 = new string('a', 64),
            TenantCode = "TENANT-8C",
            FileType = "docx",
            FileSize = 5,
            UploadedByUserId = OwnerId,
            UploadedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var renderer = new StubRenderer();
        var storage = new TrackingPrivateStorage();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context, renderer, storage).SubmitForApprovalAsync(
                ContractId, CreateRequest(), OwnerId));

        Assert.Equal(1, renderer.CallCount);
        Assert.Empty(storage.SavedKeys);
        await AssertSubmissionStateUnchangedAsync(context, expectedFileCount: 1);
    }

    private static ContractService CreateService(
        DbDtctechContext context,
        IContractSubmissionArtifactRenderer renderer,
        IPrivateFileStorage storage)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-8C",
            "Tenant Phase 8C",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "phase-8c-submit-test"
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        var audit = new ContractAuditWriter(
            context,
            tenant,
            new HttpContextAccessor { HttpContext = httpContext });
        return new ContractService(context, audit, tenant, renderer, storage);
    }

    private static DbDtctechContext CreateContext(
        IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString(),
                options => options.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new DbDtctechContext(builder.Options);
    }

    private static async Task SeedReadyContractAsync(
        DbDtctechContext context,
        bool includeManager = true,
        bool includeCustomerAccess = false)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = OwnerId,
            EmployeeFullName = "Owner",
            EmployeeType = (byte)EmployeeType.Sale,
            Status = 1,
            RowVersion = InitialRowVersion
        });
        if (includeManager)
        {
            context.TblEmployees.Add(new TblEmployee
            {
                EmployeeId = ManagerId,
                EmployeeFullName = "Manager",
                EmployeeType = (byte)EmployeeType.Manager,
                Status = 1,
                RowVersion = InitialRowVersion
            });
        }

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
            TemplateVersionId = TemplateVersionId,
            CurrentVersionId = VersionId,
            ContractCode = "HD-8C-001",
            ContractName = "Phase 8C Contract",
            Status = (byte)ContractStatus.Negotiating,
            CurrencyCode = "VND",
            Subtotal = 100m,
            TotalAmount = 100m,
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            IsLegacy = false,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = VersionId,
            ContractId = ContractId,
            VersionNo = 2,
            TemplateVersionId = TemplateVersionId,
            CurrencyCode = "VND",
            Subtotal = 100m,
            TotalAmount = 100m,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });
        context.TblContractItems.Add(new TblContractItem
        {
            ContractItemId = 8810,
            ContractId = ContractId,
            VersionId = VersionId,
            ItemType = (byte)ContractItemType.Product,
            ItemName = "Software",
            Quantity = 1m,
            UnitPrice = 100m,
            LineSubtotal = 100m,
            LineTotal = 100m,
            DisplayOrder = 1,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });
        context.TblContractTerms.Add(new TblContractTerm
        {
            TermId = 8811,
            ContractId = ContractId,
            VersionId = VersionId,
            TermCode = "GENERAL",
            TermTitle = "General",
            IsNegotiable = true,
            DisplayOrder = 1,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion
        });

        if (includeCustomerAccess)
        {
            const int linkId = 8812;
            context.TblContracts.Local.Single().CurrentCustomerAccessLinkId = linkId;
            context.TblContractCustomerAccessLinks.Add(
                new TblContractCustomerAccessLink
                {
                    CustomerAccessLinkId = linkId,
                    TenantId = TenantId,
                    ContractId = ContractId,
                    VersionId = VersionId,
                    VerificationPhoneId = 8813,
                    TokenHash = "token-hash",
                    CreatedByEmployeeId = OwnerId,
                    CreatedDate = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                    RowVersion = InitialRowVersion
                });
            context.TblContractCustomerOtpChallenges.Add(
                new TblContractCustomerOtpChallenge
                {
                    CustomerOtpChallengeId = 8814,
                    PublicChallengeId = "challenge",
                    LinkId = linkId,
                    VerificationPhoneId = 8813,
                    Purpose = "ContractAccess",
                    OtpHash = "otp-hash",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    CreatedDate = DateTime.UtcNow,
                    RowVersion = InitialRowVersion
                });
            context.TblContractCustomerAccessSessions.Add(
                new TblContractCustomerAccessSession
                {
                    CustomerAccessSessionId = 8815,
                    TenantId = TenantId,
                    LinkId = linkId,
                    ContractId = ContractId,
                    VersionId = VersionId,
                    VerificationPhoneId = 8813,
                    SessionTokenHash = "session-hash",
                    IssuedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IdleExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    HardExpiresAt = DateTime.UtcNow.AddHours(8),
                    RowVersion = InitialRowVersion
                });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static SubmitContractForApprovalRequest CreateRequest() => new()
    {
        CurrentVersionId = VersionId,
        RowVersion = Convert.ToBase64String(InitialRowVersion),
        CurrentVersionRowVersion = Convert.ToBase64String(InitialRowVersion)
    };

    private static async Task AssertSubmissionStateUnchangedAsync(
        DbDtctechContext context,
        int expectedFileCount = 0)
    {
        context.ChangeTracker.Clear();
        var contract = await context.TblContracts.AsNoTracking().SingleAsync();
        var version = await context.TblContractVersions.AsNoTracking().SingleAsync();
        Assert.Equal((byte)ContractStatus.Negotiating, contract.Status);
        Assert.False(version.IsLocked);
        Assert.Null(version.SnapshotJson);
        Assert.Empty(context.TblContractApprovalRequests);
        Assert.Equal(expectedFileCount, await context.TblFileStorages.CountAsync());
    }

    private sealed class StubRenderer : IContractSubmissionArtifactRenderer
    {
        public int CallCount { get; private set; }

        public Task<ContractSubmissionArtifactRenderResult> RenderAsync(
            int contractId,
            int employeeId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ContractSubmissionArtifactRenderResult(
                "{\"schemaVersion\":4,\"contract\":{\"contractId\":8802}}",
                4,
                TemplateVersionId,
                [0x50, 0x4B, 0x03, 0x04, 0x01],
                "HD-8C-001-submitted.docx",
                "%PDF-phase-8c"u8.ToArray(),
                "HD-8C-001-submitted.pdf"));
        }
    }

    private sealed class ThrowingRenderer(Exception exception)
        : IContractSubmissionArtifactRenderer
    {
        public Task<ContractSubmissionArtifactRenderResult> RenderAsync(
            int contractId,
            int employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ContractSubmissionArtifactRenderResult>(exception);
    }

    private sealed class TrackingPrivateStorage(int? failOnSaveNumber = null)
        : IPrivateFileStorage
    {
        private int _saveCount;
        public List<string> SavedKeys { get; } = [];
        public List<string> DeletedKeys { get; } = [];

        public async Task<StoredPrivateFile> SaveAsync(
            PrivateFileSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            _saveCount++;
            if (_saveCount == failOnSaveNumber)
            {
                throw new InvalidOperationException("Simulated storage failure.");
            }

            await using var memory = new MemoryStream();
            await request.Content.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();
            var key = $"{request.TenantCode}/{request.ObjectType}/{request.ObjectId}/{Guid.NewGuid():N}{Path.GetExtension(request.OriginalFileName)}";
            SavedKeys.Add(key);
            return new StoredPrivateFile(
                key,
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
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string tenantCode,
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class ToggleSaveFailureInterceptor : SaveChangesInterceptor
    {
        public bool ShouldFail { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            return ShouldFail
                ? ValueTask.FromException<InterceptionResult<int>>(
                    new InvalidOperationException("Simulated database failure."))
                : base.SavingChangesAsync(
                    eventData,
                    result,
                    cancellationToken);
        }
    }
}
