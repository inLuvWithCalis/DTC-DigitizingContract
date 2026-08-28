using ContractManagement.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.DTOs.Responses.File;
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
using System.Text.Json;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class ContractAttachmentAuditTests
{
    private const int TenantId = 980;
    private const int ContractId = 981;
    private const int EmployeeId = 982;

    [Fact]
    public async Task UploadAndDelete_ShouldCreateTraceableAuditEvents()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        await using var content = new MemoryStream([1, 2, 3]);
        var file = new FormFile(
            content,
            0,
            content.Length,
            "File",
            "signed-contract.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var attachment = await service.UploadAsync(
            ContractId,
            new UploadContractAttachmentRequest
            {
                File = file,
                DocumentType = 6
            },
            EmployeeId);

        var uploadedAudit = await context.TblContractAudits.SingleAsync(x =>
            x.ActionType ==
                ContractAuditActionTypes.ContractAttachmentUploaded);
        Assert.Equal(
            ContractAuditSubjectTypes.Contract,
            uploadedAudit.SubjectType);
        Assert.Equal(ContractId, uploadedAudit.SubjectId);
        using (var document = JsonDocument.Parse(
                   uploadedAudit.NewValuesJson!))
        {
            Assert.Equal(
                "signed-contract.pdf",
                document.RootElement.GetProperty("FileName").GetString());
            Assert.Equal(
                6,
                document.RootElement.GetProperty("DocumentType").GetByte());
        }

        await service.DeleteAsync(
            ContractId,
            attachment.AttachmentId,
            EmployeeId);

        var deletedAudit = await context.TblContractAudits.SingleAsync(x =>
            x.ActionType ==
                ContractAuditActionTypes.ContractAttachmentDeleted);
        Assert.Equal(ContractId, deletedAudit.SubjectId);
        using var deletedDocument = JsonDocument.Parse(
            deletedAudit.PreviousValuesJson!);
        Assert.Equal(
            "signed-contract.pdf",
            deletedDocument.RootElement.GetProperty("FileName").GetString());
        Assert.Empty(context.TblContractAttachments);
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

    private static ContractAttachmentService CreateService(
        DbDtctechContext context)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "tenant-980",
            "Tenant 980",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var writer = new ContractAuditWriter(
            context,
            tenant,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "attachment-audit-test"
                }
            });

        return new ContractAttachmentService(
            context,
            new FakeFileStorageService(),
            new AllowContractAuthorizationService(),
            writer);
    }

    private static async Task SeedAsync(DbDtctechContext context)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = EmployeeId,
            EmployeeAccount = "attachment-owner",
            EmployeeFullName = "Attachment Owner",
            EmployeeType = 1,
            Status = 1
        });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = 983,
            CustomerFullName = "Attachment Customer",
            Status = 1
        });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = 983,
            EmployeeId = EmployeeId,
            ContractCode = "HD-ATTACHMENT",
            ContractName = "Attachment contract",
            Status = 0,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private sealed class AllowContractAuthorizationService
        : IContractResourceAuthorizationService
    {
        public Task EnsureCanReadAsync(
            int contractId,
            int employeeId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task EnsureCanWriteAsync(
            int contractId,
            int employeeId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<FileStorageResponse> UploadAsync(
            IFormFile file,
            string objectType,
            int objectId,
            int uploadedBy) => Task.FromResult(new FileStorageResponse
            {
                FileId = 1,
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = file.FileName,
                FilePath = "contracts/signed-contract.pdf",
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedByUserId = uploadedBy,
                UploadedDate = DateTime.UtcNow
            });

        public Task<(Stream Stream, string FileName)?> DownloadAsync(
            int fileId) => Task.FromResult<
                (Stream Stream, string FileName)?>(null);

        public Task<List<FileStorageResponse>> GetByObjectAsync(
            string objectType,
            int objectId) => Task.FromResult(new List<FileStorageResponse>());

        public Task DeleteAsync(int fileId) => Task.CompletedTask;

        public Task DeleteUploadedArtifactAsync(FileStorageResponse file) =>
            Task.CompletedTask;
    }
}
