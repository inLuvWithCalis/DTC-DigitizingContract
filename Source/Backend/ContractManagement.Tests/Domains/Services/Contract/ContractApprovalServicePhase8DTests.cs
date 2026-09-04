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

public sealed class ContractApprovalServicePhase8DTests
{
    private const int ContractId = 9101;
    private const int VersionId = 9102;
    private const int ApprovalRequestId = 9103;
    private const int OwnerId = 9104;
    private const int ManagerAId = 9105;
    private const int ManagerBId = 9106;
    private const int TenantId = 9107;
    private static readonly byte[] InitialRowVersion =
        [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Approve_VerifiesArtifactsAndMovesToPendingSignature()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var audit = CreateAuditWriter(context);
        var service = CreateService(context, storage, audit);

        var response = await service.DecideAsync(
            ApprovalRequestId,
            ApprovalRequestStatus.Approved,
            DecisionRequest("Đã kiểm tra bản PDF."),
            ManagerAId);
        var detail = await service.GetDetailAsync(
            ApprovalRequestId,
            ManagerAId);
        var contractHistory = await service.GetContractHistoryAsync(
            ContractId,
            OwnerId);

        var contract = await context.TblContracts.AsNoTracking().SingleAsync();
        var approval = await context.TblContractApprovalRequests
            .AsNoTracking()
            .SingleAsync();
        var history = await context.TblApprovalHistories
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(ContractStatus.PendingSignature, response.ContractStatus);
        Assert.Equal((byte)ContractStatus.PendingSignature, contract.Status);
        Assert.Equal((byte)ApprovalRequestStatus.Approved, approval.Status);
        Assert.Equal(ManagerAId, approval.ResolvedByEmployeeId);
        Assert.Equal("Approved", history.ApprovalAction);
        Assert.Equal(ApprovalRequestId, history.ObjectId);
        Assert.Equal(2, detail.Artifacts.Count);
        Assert.Equal(ApprovalRequestStatus.Approved, detail.Status);
        Assert.Equal(
            ApprovalRequestId,
            Assert.Single(contractHistory).ApprovalRequestId);
        var contractAudit = await context.TblContractAudits
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(
            ContractAuditActionTypes.ApprovalApproved,
            contractAudit.ActionType);
        Assert.Equal(
            ContractAuditSubjectTypes.ApprovalRequest,
            contractAudit.SubjectType);
        Assert.Equal(ApprovalRequestId, contractAudit.SubjectId);
    }

    [Fact]
    public async Task TwoManagers_OnlyFirstDecisionWins()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(
            context,
            storage,
            new RecordingAuditWriter());
        var staleRequest = DecisionRequest("Manager B từ chối.");

        await service.DecideAsync(
            ApprovalRequestId,
            ApprovalRequestStatus.Returned,
            DecisionRequest("Cần chỉnh lại điều khoản."),
            ManagerAId);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DecideAsync(
                ApprovalRequestId,
                ApprovalRequestStatus.Rejected,
                staleRequest,
                ManagerBId));

        Assert.Equal(
            ContractApprovalErrorCodes.ApprovalRequestAlreadyResolved,
            exception.Code);
        Assert.Single(context.TblApprovalHistories);
        Assert.Equal(
            (byte)ApprovalRequestStatus.Returned,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
    }

    [Fact]
    public async Task Withdraw_OwnerReasonRequiredAndVersionStaysLocked()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(
            context,
            storage,
            new RecordingAuditWriter());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.WithdrawAsync(
                ApprovalRequestId,
                new WithdrawContractApprovalRequest
                {
                    RowVersion = EncodedRowVersion(),
                    Reason = " "
                },
                OwnerId));

        var response = await service.WithdrawAsync(
            ApprovalRequestId,
            new WithdrawContractApprovalRequest
            {
                RowVersion = EncodedRowVersion(),
                Reason = "Cập nhật lại phạm vi triển khai."
            },
            OwnerId);

        Assert.Equal(ContractStatus.Negotiating, response.ContractStatus);
        Assert.True((await context.TblContractVersions
            .AsNoTracking()
            .SingleAsync()).IsLocked);
        Assert.Equal(
            (byte)ApprovalRequestStatus.Withdrawn,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
    }

    [Fact]
    public async Task Approve_MissingPhysicalArtifact_DoesNotResolveRequest()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        storage.Remove("tenant-phase8d/ContractVersionArtifact/9102/file.pdf");

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(context, storage, new RecordingAuditWriter())
                .DecideAsync(
                    ApprovalRequestId,
                    ApprovalRequestStatus.Approved,
                    DecisionRequest(null),
                    ManagerAId));

        Assert.Equal(
            ContractApprovalErrorCodes.ApprovalArtifactMissing,
            exception.Code);
        Assert.Equal(
            (byte)ApprovalRequestStatus.Pending,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
        Assert.Empty(context.TblApprovalHistories);
    }

    [Fact]
    public async Task Inbox_ExcludesSubmitterAndReturnsPendingRequests()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(
            context,
            storage,
            new RecordingAuditWriter());

        var inbox = await service.GetInboxAsync(
            new ContractApprovalInboxFilterRequest(),
            ManagerAId);

        var item = Assert.Single(inbox.Items);
        Assert.Equal(ApprovalRequestId, item.ApprovalRequestId);
        Assert.Equal(1, item.VersionNo);
        Assert.Equal("Owner", item.SubmittedByEmployeeName);
    }

    [Theory]
    [InlineData(
        ApprovalRequestStatus.Returned,
        ContractStatus.Negotiating,
        ContractAuditActionTypes.ApprovalReturned)]
    [InlineData(
        ApprovalRequestStatus.Rejected,
        ContractStatus.Rejected,
        ContractAuditActionTypes.ApprovalRejected)]
    public async Task ReturnOrReject_WithReason_ResolvesRequestAndAuditsResult(
        ApprovalRequestStatus decision,
        ContractStatus expectedContractStatus,
        string expectedAuditAction)
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(context, storage, CreateAuditWriter(context));

        var response = await service.DecideAsync(
            ApprovalRequestId,
            decision,
            DecisionRequest("Nội dung cần xử lý trước bước tiếp theo."),
            ManagerAId);

        Assert.Equal(expectedContractStatus, response.ContractStatus);
        Assert.Equal(
            (byte)decision,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
        Assert.Contains(
            context.TblContractAudits,
            audit => audit.ActionType == expectedAuditAction
                && audit.SubjectType == ContractAuditSubjectTypes.ApprovalRequest
                && audit.SubjectId == ApprovalRequestId);
    }

    [Theory]
    [InlineData(ApprovalRequestStatus.Returned)]
    [InlineData(ApprovalRequestStatus.Rejected)]
    public async Task ReturnOrReject_WithoutReason_DoesNotResolveRequest(
        ApprovalRequestStatus decision)
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(
            context,
            storage,
            new RecordingAuditWriter());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DecideAsync(
                ApprovalRequestId,
                decision,
                DecisionRequest(" "),
                ManagerAId));

        Assert.Equal(
            ContractApprovalErrorCodes.ApprovalReasonRequired,
            exception.Code);
        Assert.Equal(
            (byte)ApprovalRequestStatus.Pending,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
        Assert.Empty(context.TblApprovalHistories);
    }

    [Fact]
    public async Task Submitter_CannotApproveOwnRequest()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var submitter = await context.TblEmployees.SingleAsync(employee =>
            employee.EmployeeId == OwnerId);
        submitter.EmployeeType = (byte)EmployeeType.Manager;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(context, storage, new RecordingAuditWriter())
                .DecideAsync(
                    ApprovalRequestId,
                    ApprovalRequestStatus.Approved,
                    DecisionRequest(null),
                    OwnerId));

        Assert.Equal(ContractApprovalErrorCodes.SelfApprovalDenied, exception.Code);
        Assert.Equal(
            (byte)ApprovalRequestStatus.Pending,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
    }

    [Fact]
    public async Task StaleRowVersion_DoesNotResolveApprovalRequest()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var request = DecisionRequest("Dữ liệu trên màn hình đã cũ.");
        request.RowVersion = Convert.ToBase64String(
            [8, 7, 6, 5, 4, 3, 2, 1]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            CreateService(context, storage, new RecordingAuditWriter())
                .DecideAsync(
                    ApprovalRequestId,
                    ApprovalRequestStatus.Rejected,
                    request,
                    ManagerAId));

        Assert.Equal(
            (byte)ApprovalRequestStatus.Pending,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
        Assert.Empty(context.TblApprovalHistories);
    }

    [Fact]
    public async Task Inbox_FiltersSubmittedDateInclusively()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(
            context,
            storage,
            new RecordingAuditWriter());
        var submittedDate = (await context.TblContractApprovalRequests
            .AsNoTracking()
            .SingleAsync()).SubmittedDate.Date;

        var included = await service.GetInboxAsync(
            new ContractApprovalInboxFilterRequest
            {
                FromDate = submittedDate,
                ToDate = submittedDate
            },
            ManagerAId);
        var excluded = await service.GetInboxAsync(
            new ContractApprovalInboxFilterRequest
            {
                FromDate = submittedDate.AddDays(1),
                ToDate = submittedDate.AddDays(1)
            },
            ManagerAId);

        Assert.Single(included.Items);
        Assert.Empty(excluded.Items);
    }

    [Fact]
    public async Task BulkDecision_ReturnsPerItemSuccessAndFailure()
    {
        await using var context = CreateContext();
        var storage = await SeedPendingApprovalAsync(context);
        var service = CreateService(context, storage, CreateAuditWriter(context));

        var response = await service.DecideBulkAsync(
            new ContractApprovalBulkDecisionRequest
            {
                Decision = ApprovalRequestStatus.Returned,
                Comment = "Cần chỉnh sửa trước khi gửi lại.",
                Items =
                [
                    new ContractApprovalBulkDecisionItemRequest
                    {
                        ApprovalRequestId = ApprovalRequestId,
                        RowVersion = EncodedRowVersion()
                    },
                    new ContractApprovalBulkDecisionItemRequest
                    {
                        ApprovalRequestId = ApprovalRequestId + 999,
                        RowVersion = EncodedRowVersion()
                    }
                ]
            },
            ManagerAId);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(1, response.SuccessCount);
        Assert.Equal(1, response.FailureCount);
        Assert.True(response.Items[0].Success);
        Assert.False(response.Items[1].Success);
        Assert.Equal(
            AuthorizationErrorCodes.ResourceNotFound,
            response.Items[1].ErrorCode);
        Assert.Equal(
            (byte)ApprovalRequestStatus.Returned,
            (await context.TblContractApprovalRequests
                .AsNoTracking()
                .SingleAsync()).Status);
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

    private static ContractApprovalService CreateService(
        DbDtctechContext context,
        MemoryPrivateFileStorage storage,
        IContractAuditWriter auditWriter) => new(
            context,
            new ContractResourceAuthorizationService(context),
            auditWriter,
            storage);

    private static IContractAuditWriter CreateAuditWriter(
        DbDtctechContext context)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-8D",
            "Tenant Phase 8D",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "phase-8d-approval-test"
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        return new ContractAuditWriter(
            context,
            tenant,
            new HttpContextAccessor { HttpContext = httpContext });
    }

    private static async Task<MemoryPrivateFileStorage>
        SeedPendingApprovalAsync(DbDtctechContext context)
    {
        context.TblEmployees.AddRange(
            Employee(OwnerId, "Owner", EmployeeType.Sale),
            Employee(ManagerAId, "Manager A", EmployeeType.Manager),
            Employee(ManagerBId, "Manager B", EmployeeType.Manager));
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = 99,
            EmployeeId = OwnerId,
            ContractType = (byte)ContractType.SoftwareSupply,
            CurrentVersionId = VersionId,
            ContractCode = "HD-8D",
            ContractName = "Phase 8D",
            Status = (byte)ContractStatus.PendingApproval,
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
            SnapshotJson = "{\"schemaVersion\":4}",
            SnapshotHash = new string('a', 64),
            IsLocked = true,
            LockedDate = DateTime.UtcNow,
            LockedByEmployeeId = OwnerId,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion.ToArray()
        });
        context.TblContractApprovalRequests.Add(
            new TblContractApprovalRequest
            {
                ApprovalRequestId = ApprovalRequestId,
                ContractId = ContractId,
                VersionId = VersionId,
                Status = (byte)ApprovalRequestStatus.Pending,
                SubmittedByEmployeeId = OwnerId,
                SubmittedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion.ToArray()
            });

        var storage = new MemoryPrivateFileStorage();
        AddArtifact(context, storage, "docx", [0x50, 0x4B, 0x03, 0x04]);
        AddArtifact(context, storage, "pdf", "%PDF-1.7"u8.ToArray());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return storage;
    }

    private static void AddArtifact(
        DbDtctechContext context,
        MemoryPrivateFileStorage storage,
        string fileType,
        byte[] content)
    {
        var key =
            $"tenant-phase8d/ContractVersionArtifact/{VersionId}/file.{fileType}";
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        storage.Add(key, content);
        context.TblFileStorages.Add(new TblFileStorage
        {
            ObjectType = "ContractVersionArtifact",
            ObjectId = VersionId,
            FileName = $"submitted.{fileType}",
            FilePath = string.Empty,
            StorageKey = key,
            TenantCode = "tenant-phase8d",
            ContentType = fileType == "pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Sha256 = hash,
            FileType = fileType,
            FileSize = content.Length,
            UploadedByUserId = OwnerId,
            UploadedDate = DateTime.UtcNow
        });
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
            Status = 1
        };

    private static ContractApprovalDecisionRequest DecisionRequest(
        string? comment) => new()
        {
            RowVersion = EncodedRowVersion(),
            Comment = comment
        };

    private static string EncodedRowVersion() =>
        Convert.ToBase64String(InitialRowVersion);

    private sealed class RecordingAuditWriter : IContractAuditWriter
    {
        public List<EmployeeContractAuditWriteRequest> Requests { get; } = [];

        public void StageAudits(
            IReadOnlyCollection<ContractAuditWriteRequest> requests)
        {
        }

        public void StageEmployeeAudits(
            IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests) =>
            Requests.AddRange(requests);
    }

    private sealed class MemoryPrivateFileStorage : IPrivateFileStorage
    {
        private readonly Dictionary<string, byte[]> _files =
            new(StringComparer.Ordinal);

        public void Add(string key, byte[] content) =>
            _files[key] = content.ToArray();

        public void Remove(string key) => _files.Remove(key);

        public Task<StoredPrivateFile> SaveAsync(
            PrivateFileSaveRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(
            string tenantCode,
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            if (!_files.TryGetValue(storageKey, out var content))
            {
                throw new FileNotFoundException();
            }

            return Task.FromResult<Stream>(
                new MemoryStream(content, writable: false));
        }

        public Task DeleteAsync(
            string tenantCode,
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            _files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }
}
