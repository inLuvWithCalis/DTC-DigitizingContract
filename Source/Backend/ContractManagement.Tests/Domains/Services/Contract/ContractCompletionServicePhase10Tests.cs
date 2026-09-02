using System.Net;
using System.Security.Cryptography;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
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

public sealed class ContractCompletionServicePhase10Tests
{
    private const int TenantId = 12001;
    private const int OwnerId = 12002;
    private const int ManagerId = 12003;
    private const int OtherEmployeeId = 12004;
    private const int CustomerId = 12005;
    private const int ContractId = 12006;
    private const int VersionId = 12007;
    private const decimal ContractTotal = 1_000m;

    private static readonly byte[] InitialRowVersion =
        [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Readiness_ReportsEveryMissingCompletionCondition()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.PendingSignature,
            includeSignedEvidence: false);

        var readiness = await CreateService(context, new TrackingPrivateStorage())
            .GetReadinessAsync(ContractId, OwnerId);

        Assert.False(readiness.Ready);
        Assert.False(readiness.Signed);
        Assert.False(readiness.AcceptanceEvidenceAvailable);
        Assert.Equal(ContractTotal, readiness.RemainingAmount);
        Assert.Equal(
            ["NOT_SIGNED", "ACCEPTANCE_MISSING", "PAYMENT_NOT_FULLY_PAID"],
            readiness.Blockers.Select(blocker => blocker.Code));
    }

    [Fact]
    public async Task UploadAcceptance_PersistsFileEvidenceAndAudit()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed);
        var storage = new TrackingPrivateStorage();

        var response = await CreateService(context, storage)
            .UploadAcceptanceAsync(
                ContractId,
                AcceptanceRequest(VersionId),
                OwnerId);

        var evidence = await context.TblContractAcceptanceEvidences
            .AsNoTracking()
            .SingleAsync();
        var file = await context.TblFileStorages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.FileId == evidence.FileId);
        var audit = await context.TblContractAudits
            .AsNoTracking()
            .SingleAsync(candidate => candidate.ActionType ==
                ContractAuditActionTypes.AcceptanceEvidenceUploaded);

        Assert.Equal(evidence.AcceptanceEvidenceId, response.AcceptanceEvidenceId);
        Assert.Equal("ContractAcceptanceEvidence", file.ObjectType);
        Assert.Equal(ContractId, file.ObjectId);
        Assert.Equal(64, file.Sha256?.Length);
        Assert.Equal(ContractAuditSubjectTypes.AcceptanceEvidence, audit.SubjectType);
        Assert.Equal(evidence.AcceptanceEvidenceId, audit.SubjectId);
        Assert.Single(storage.SavedKeys);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task UploadAcceptance_StateChanged_DeletesStagedFileAndDoesNotPersistMetadata()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed);
        var storage = new TrackingPrivateStorage();

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(context, storage).UploadAcceptanceAsync(
                ContractId,
                AcceptanceRequest(VersionId + 1),
                OwnerId));

        Assert.Equal("CompletionStateChanged", exception.Code);
        Assert.Single(storage.SavedKeys);
        Assert.Single(storage.DeletedKeys);
        Assert.Empty(context.TblContractAcceptanceEvidences);
        Assert.DoesNotContain(
            context.TblFileStorages,
            candidate => candidate.ObjectType == "ContractAcceptanceEvidence");
    }

    [Fact]
    public async Task Payments_PartialThenFull_RecalculateReadinessAndAuditEachEntry()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed, includeAcceptance: true);
        var service = CreateService(context, new TrackingPrivateStorage());

        var partial = await service.AddPaymentAsync(
            ContractId,
            PaymentRequest(400m, "ref-001"),
            OwnerId);
        var partialReadiness = await service.GetReadinessAsync(
            ContractId,
            OwnerId);
        var final = await service.AddPaymentAsync(
            ContractId,
            PaymentRequest(600m, "ref-002"),
            OwnerId);
        var finalReadiness = await service.GetReadinessAsync(
            ContractId,
            OwnerId);

        Assert.Equal(ContractPaymentStatus.Active, partial.Status);
        Assert.Equal(ContractPaymentStatus.Active, final.Status);
        Assert.Equal(400m, partialReadiness.PaidAmount);
        Assert.Equal(600m, partialReadiness.RemainingAmount);
        Assert.False(partialReadiness.Ready);
        Assert.Equal(ContractTotal, finalReadiness.PaidAmount);
        Assert.Equal(0m, finalReadiness.RemainingAmount);
        Assert.True(finalReadiness.Ready);
        Assert.Equal(
            2,
            await context.TblContractAudits.CountAsync(candidate =>
                candidate.ActionType == ContractAuditActionTypes.PaymentAdded));
    }

    [Fact]
    public async Task Payment_DuplicateReferenceAndOverpayment_AreRejectedWithoutExtraRows()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed, includeAcceptance: true);
        var service = CreateService(context, new TrackingPrivateStorage());

        await service.AddPaymentAsync(
            ContractId,
            PaymentRequest(700m, "bank-001"),
            OwnerId);

        var duplicate = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddPaymentAsync(
                ContractId,
                PaymentRequest(100m, "BANK-001"),
                OwnerId));
        var overpayment = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddPaymentAsync(
                ContractId,
                PaymentRequest(301m, "BANK-002"),
                OwnerId));

        Assert.Equal("PaymentReferenceDuplicated", duplicate.Code);
        Assert.Equal("PaymentExceedsContractTotal", overpayment.Code);
        Assert.Single(context.TblContractPaymentLedgers);
    }

    [Fact]
    public async Task Payment_CurrencyMismatch_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed, includeAcceptance: true);
        var request = PaymentRequest(100m, "currency-mismatch");
        request.CurrencyCode = "USD";

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(context, new TrackingPrivateStorage())
                .AddPaymentAsync(ContractId, request, OwnerId));

        Assert.Equal("PaymentCurrencyMismatch", exception.Code);
        Assert.Empty(context.TblContractPaymentLedgers);
    }

    [Fact]
    public async Task VoidPayment_RequiresReasonAndRemovesAmountFromReadiness()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Signed,
            includeAcceptance: true,
            activePayments: [ContractTotal]);
        var service = CreateService(context, new TrackingPrivateStorage());
        var payment = await context.TblContractPaymentLedgers
            .AsNoTracking()
            .SingleAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.VoidPaymentAsync(
                ContractId,
                payment.ContractPaymentId,
                VoidRequest(payment.RowVersion, " "),
                OwnerId));

        var response = await service.VoidPaymentAsync(
            ContractId,
            payment.ContractPaymentId,
            VoidRequest(payment.RowVersion, "Chứng từ ngân hàng nhập nhầm."),
            OwnerId);
        var readiness = await service.GetReadinessAsync(ContractId, OwnerId);

        Assert.Equal(ContractPaymentStatus.Voided, response.Status);
        Assert.Equal("Chứng từ ngân hàng nhập nhầm.", response.VoidReason);
        Assert.Equal(0m, readiness.PaidAmount);
        Assert.False(readiness.Ready);
        Assert.Contains(
            context.TblContractAudits,
            candidate => candidate.ActionType ==
                ContractAuditActionTypes.PaymentVoided);
    }

    [Fact]
    public async Task Complete_ManagerMovesReadyContractToCompletedAndWritesAudit()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Signed,
            includeAcceptance: true,
            activePayments: [ContractTotal]);

        var response = await CreateService(context, new TrackingPrivateStorage())
            .CompleteAsync(ContractId, CompleteRequest(), ManagerId);

        Assert.Equal(ContractStatus.Completed, response.ContractStatus);
        Assert.False(response.Readiness.Ready);
        Assert.Equal(
            (byte)ContractStatus.Completed,
            (await context.TblContracts.AsNoTracking().SingleAsync()).Status);
        Assert.Contains(
            context.TblContractAudits,
            candidate => candidate.ActionType ==
                ContractAuditActionTypes.ContractCompleted
                && candidate.ActorEmployeeId == ManagerId);
    }

    [Fact]
    public async Task Complete_OwnerIsForbiddenAndNotReadyContractRemainsSigned()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed);
        var service = CreateService(context, new TrackingPrivateStorage());

        var forbidden = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CompleteAsync(ContractId, CompleteRequest(), OwnerId));
        var notReady = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CompleteAsync(ContractId, CompleteRequest(), ManagerId));

        Assert.Equal("PermissionDenied", forbidden.Code);
        Assert.Equal("ContractNotReadyForCompletion", notReady.Code);
        Assert.Equal(
            (byte)ContractStatus.Signed,
            (await context.TblContracts.AsNoTracking().SingleAsync()).Status);
        Assert.DoesNotContain(
            context.TblContractAudits,
            candidate => candidate.ActionType ==
                ContractAuditActionTypes.ContractCompleted);
    }

    [Fact]
    public async Task Complete_RecomputesReadinessInsideMutation()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Signed,
            includeAcceptance: true,
            activePayments: [ContractTotal]);
        var service = CreateService(context, new TrackingPrivateStorage());

        Assert.True((await service.GetReadinessAsync(ContractId, ManagerId)).Ready);
        var payment = await context.TblContractPaymentLedgers.SingleAsync();
        payment.Status = (byte)ContractPaymentStatus.Voided;
        payment.VoidReason = "Mô phỏng dữ liệu đổi giữa GET và POST.";
        payment.VoidedByEmployeeId = OwnerId;
        payment.VoidedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CompleteAsync(ContractId, CompleteRequest(), ManagerId));

        Assert.Equal("ContractNotReadyForCompletion", exception.Code);
        Assert.Equal(
            (byte)ContractStatus.Signed,
            (await context.TblContracts.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Complete_StaleContractRowVersion_DoesNotChangeState()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Signed,
            includeAcceptance: true,
            activePayments: [ContractTotal]);
        var request = CompleteRequest();
        request.ContractRowVersion = Convert.ToBase64String(
            [8, 7, 6, 5, 4, 3, 2, 1]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            CreateService(context, new TrackingPrivateStorage())
                .CompleteAsync(ContractId, request, ManagerId));

        Assert.Equal(
            (byte)ContractStatus.Signed,
            (await context.TblContracts.AsNoTracking().SingleAsync()).Status);
        Assert.DoesNotContain(
            context.TblContractAudits,
            audit => audit.ActionType == ContractAuditActionTypes.ContractCompleted);
    }

    [Fact]
    public async Task CompletedContract_RejectsAllCompletionMutations()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            ContractStatus.Completed,
            includeAcceptance: true,
            activePayments: [ContractTotal]);
        var storage = new TrackingPrivateStorage();
        var service = CreateService(context, storage);
        var payment = await context.TblContractPaymentLedgers
            .AsNoTracking()
            .SingleAsync();

        var acceptance = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UploadAcceptanceAsync(
                ContractId,
                AcceptanceRequest(VersionId),
                OwnerId));
        var addPayment = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddPaymentAsync(
                ContractId,
                PaymentRequest(1m, "late-payment"),
                OwnerId));
        var voidPayment = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.VoidPaymentAsync(
                ContractId,
                payment.ContractPaymentId,
                VoidRequest(payment.RowVersion, "Không được sửa sau hoàn tất."),
                OwnerId));
        var completeAgain = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CompleteAsync(ContractId, CompleteRequest(), ManagerId));

        Assert.Equal("ContractMustBeSigned", acceptance.Code);
        Assert.Equal("ContractMustBeSigned", addPayment.Code);
        Assert.Equal("ContractMustBeSigned", voidPayment.Code);
        Assert.Equal("ContractNotReadyForCompletion", completeAgain.Code);
        Assert.Single(storage.SavedKeys);
        Assert.Single(storage.DeletedKeys);
        Assert.Single(context.TblContractPaymentLedgers);
    }

    [Fact]
    public async Task OwnerCannotMutateAnotherEmployeesCompletionData()
    {
        await using var context = CreateContext();
        await SeedAsync(context, ContractStatus.Signed);
        var storage = new TrackingPrivateStorage();

        var exception = await Assert.ThrowsAsync<RbacOperationException>(() =>
            CreateService(context, storage).AddPaymentAsync(
                ContractId,
                PaymentRequest(100m, "unauthorized"),
                OtherEmployeeId));

        Assert.Equal(AuthorizationErrorCodes.ResourceNotFound, exception.Code);
        Assert.Empty(storage.SavedKeys);
        Assert.Empty(context.TblContractPaymentLedgers);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString(),
                databaseOptions => databaseOptions.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static ContractCompletionService CreateService(
        DbDtctechContext context,
        IPrivateFileStorage storage)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-PHASE12",
            "Tenant Phase 12",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "phase-12-completion-test"
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        return new ContractCompletionService(
            context,
            new ContractResourceAuthorizationService(context),
            new ContractAuditWriter(
                context,
                tenant,
                new HttpContextAccessor { HttpContext = httpContext }),
            storage,
            tenant);
    }

    private static async Task SeedAsync(
        DbDtctechContext context,
        ContractStatus status,
        bool includeSignedEvidence = true,
        bool includeAcceptance = false,
        IReadOnlyCollection<decimal>? activePayments = null)
    {
        context.TblEmployees.AddRange(
            Employee(OwnerId, "Owner", EmployeeType.Sale),
            Employee(ManagerId, "Manager", EmployeeType.Manager),
            Employee(OtherEmployeeId, "Other Owner", EmployeeType.Sale));
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Customer Phase 12",
            Status = 1
        });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = CustomerId,
            EmployeeId = OwnerId,
            ContractType = (byte)ContractType.SoftwareSupply,
            CurrentVersionId = VersionId,
            ContractCode = "HD-PHASE12",
            ContractName = "Phase 12 Completion Contract",
            Status = (byte)status,
            TotalAmount = ContractTotal,
            CurrencyCode = "VND",
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion.ToArray()
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = VersionId,
            ContractId = ContractId,
            VersionNo = 1,
            CurrencyCode = "VND",
            TotalAmount = ContractTotal,
            SnapshotJson = "{\"schemaVersion\":4}",
            SnapshotHash = new string('a', 64),
            IsLocked = true,
            LockedDate = DateTime.UtcNow.AddDays(-1),
            LockedByEmployeeId = OwnerId,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow.AddDays(-2),
            RowVersion = InitialRowVersion.ToArray()
        });

        if (includeSignedEvidence)
        {
            context.TblFileStorages.Add(FileMetadata(
                12008,
                "ContractSignedEvidence",
                "signed.pdf"));
            context.TblContractSignedEvidences.Add(new TblContractSignedEvidence
            {
                SignedEvidenceId = 12009,
                ContractId = ContractId,
                VersionId = VersionId,
                FileId = 12008,
                Status = (byte)SignedEvidenceStatus.Active,
                ProviderSignerName = "Provider Signer",
                ProviderSignerTitle = "Director",
                ProviderSigningDate = DateTime.UtcNow.AddDays(-2),
                CustomerSignerName = "Customer Signer",
                CustomerSignerTitle = "CEO",
                CustomerSigningDate = DateTime.UtcNow.AddDays(-1),
                UploadedByEmployeeId = OwnerId,
                UploadedAt = DateTime.UtcNow.AddDays(-1),
                RowVersion = InitialRowVersion.ToArray()
            });
        }

        if (includeAcceptance)
        {
            context.TblFileStorages.Add(FileMetadata(
                12010,
                "ContractAcceptanceEvidence",
                "acceptance.pdf"));
            context.TblContractAcceptanceEvidences.Add(
                new TblContractAcceptanceEvidence
                {
                    AcceptanceEvidenceId = 12011,
                    ContractId = ContractId,
                    VersionId = VersionId,
                    FileId = 12010,
                    UploadedByEmployeeId = OwnerId,
                    UploadedAt = DateTime.UtcNow,
                    RowVersion = InitialRowVersion.ToArray()
                });
        }

        var paymentId = 12020;
        foreach (var amount in activePayments ?? [])
        {
            context.TblContractPaymentLedgers.Add(new TblContractPaymentLedger
            {
                ContractPaymentId = paymentId,
                ContractId = ContractId,
                VersionId = VersionId,
                PaymentDate = DateTime.UtcNow.Date,
                Amount = amount,
                CurrencyCode = "VND",
                PaymentMethod = "BankTransfer",
                ReferenceCode = $"SEED-{paymentId}",
                Status = (byte)ContractPaymentStatus.Active,
                CreatedByEmployeeId = OwnerId,
                CreatedAt = DateTime.UtcNow,
                RowVersion = InitialRowVersion.ToArray()
            });
            paymentId++;
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static TblEmployee Employee(
        int id,
        string name,
        EmployeeType type) => new()
        {
            EmployeeId = id,
            EmployeeAccount = $"employee-{id}",
            EmployeeFullName = name,
            EmployeeType = (byte)type,
            Status = 1,
            RowVersion = InitialRowVersion.ToArray()
        };

    private static TblFileStorage FileMetadata(
        int fileId,
        string objectType,
        string fileName) => new()
        {
            FileId = fileId,
            ObjectType = objectType,
            ObjectId = ContractId,
            FileName = fileName,
            FilePath = string.Empty,
            StorageKey = $"TENANT-PHASE12/{objectType}/{ContractId}/{fileName}",
            ContentType = "application/pdf",
            Sha256 = new string('b', 64),
            TenantCode = "TENANT-PHASE12",
            FileType = "pdf",
            FileSize = 100,
            UploadedByUserId = OwnerId,
            UploadedDate = DateTime.UtcNow
        };

    private static UploadContractAcceptanceEvidenceRequest AcceptanceRequest(
        int versionId) => new()
        {
            File = FormFile("acceptance.pdf", "application/pdf"),
            CurrentVersionId = versionId,
            ContractRowVersion = Encode(InitialRowVersion),
            VersionRowVersion = Encode(InitialRowVersion)
        };

    private static AddContractPaymentRequest PaymentRequest(
        decimal amount,
        string referenceCode) => new()
        {
            CurrentVersionId = VersionId,
            ContractRowVersion = Encode(InitialRowVersion),
            VersionRowVersion = Encode(InitialRowVersion),
            PaymentDate = DateTime.UtcNow.Date,
            Amount = amount,
            CurrencyCode = "VND",
            PaymentMethod = "BankTransfer",
            ReferenceCode = referenceCode
        };

    private static VoidContractPaymentRequest VoidRequest(
        byte[] rowVersion,
        string reason) => new()
        {
            ContractRowVersion = Encode(InitialRowVersion),
            VersionRowVersion = Encode(InitialRowVersion),
            PaymentRowVersion = Encode(rowVersion),
            Reason = reason
        };

    private static CompleteContractRequest CompleteRequest() => new()
    {
        CurrentVersionId = VersionId,
        ContractRowVersion = Encode(InitialRowVersion),
        VersionRowVersion = Encode(InitialRowVersion)
    };

    private static IFormFile FormFile(string fileName, string contentType)
    {
        var bytes = "%PDF-phase12"u8.ToArray();
        return new FormFile(
            new MemoryStream(bytes),
            0,
            bytes.Length,
            "File",
            fileName)
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
