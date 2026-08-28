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
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace ContractManagement.Tests.Domains.Services.Contract;

public class ContractServiceResponsibilityTransferTests
{
    private const int ContractId = 11;
    private const int VersionId = 12;
    private const int CustomerId = 13;
    private const int NewCustomerId = 14;
    private const int CreatorEmployeeId = 90;
    private const int CurrentResponsibleEmployeeId = 101;
    private const int NewResponsibleEmployeeId = 202;
    private const int ManagerEmployeeId = 303;
    private const int AdminOfficerEmployeeId = 304;
    private const int OtherEmployeeId = 404;
    private const int TenantId = 601;
    private const string CorrelationId =
        "slice-03-transfer-correlation";

    [Fact]
    public async Task CurrentResponsible_ShouldTransferSuccessfully()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);

        var service = CreateService(context);
        var response = await service.TransferResponsibilityAsync(
            ContractId,
            CreateTransferRequest(),
            CurrentResponsibleEmployeeId);

        var contract = await context.TblContracts.SingleAsync();
        var audit = await context.TblContractAudits.SingleAsync();

        Assert.Equal(ContractId, response.ContractId);
        Assert.Equal(
            CurrentResponsibleEmployeeId,
            response.PreviousResponsibleEmployeeId);
        Assert.Equal(
            NewResponsibleEmployeeId,
            response.ResponsibleEmployeeId);
        Assert.Equal(
            CurrentResponsibleEmployeeId,
            response.TransferredByEmployeeId);
        Assert.False(string.IsNullOrWhiteSpace(response.RowVersion));
        Assert.Equal(response.TransferredAt, audit.OccurredAt);
        Assert.Equal(NewResponsibleEmployeeId, contract.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.CreatedEmployeeId);
        Assert.Equal(
            CurrentResponsibleEmployeeId,
            contract.UpdatedEmployeeId);
        AssertTransferAudit(
            audit,
            CurrentResponsibleEmployeeId,
            CurrentResponsibleEmployeeId,
            NewResponsibleEmployeeId,
            ContractAuditResults.Succeeded,
            expectedReason: "Bàn giao phụ trách");
    }

    [Theory]
    [InlineData(EmployeeType.Manager, ManagerEmployeeId)]
    [InlineData(EmployeeType.AdminOfficer, AdminOfficerEmployeeId)]
    public async Task PrivilegedEmployee_ShouldTransferSuccessfully(
        EmployeeType employeeType,
        int actorEmployeeId)
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Accountant);
        await AddEmployeeAsync(
            context,
            actorEmployeeId,
            employeeType);

        var response = await CreateService(context)
            .TransferResponsibilityAsync(
                ContractId,
                CreateTransferRequest(),
                actorEmployeeId);

        Assert.Equal(
            NewResponsibleEmployeeId,
            response.ResponsibleEmployeeId);
        Assert.Equal(
            actorEmployeeId,
            response.TransferredByEmployeeId);
    }

    [Fact]
    public async Task OtherEmployee_ShouldBeRejected()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);
        await AddEmployeeAsync(
            context,
            OtherEmployeeId,
            EmployeeType.Sale);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    CreateTransferRequest(),
                    OtherEmployeeId));

        Assert.Equal(
            CurrentResponsibleEmployeeId,
            (await context.TblContracts.SingleAsync()).EmployeeId);
        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task MissingTarget_ShouldBeRejected()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    CreateTransferRequest(),
                    CurrentResponsibleEmployeeId));

        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task InactiveTarget_ShouldBeRejected()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Marketing,
            status: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    CreateTransferRequest(),
                    CurrentResponsibleEmployeeId));

        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task TargetFromAnotherTenantContext_ShouldBeRejected()
    {
        await using var currentContext = CreateContext();
        await SeedContractAsync(currentContext);

        await using var otherContext = CreateContext();
        await AddEmployeeAsync(
            otherContext,
            NewResponsibleEmployeeId,
            EmployeeType.Manager);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService(currentContext)
                .TransferResponsibilityAsync(
                    ContractId,
                    CreateTransferRequest(),
                    CurrentResponsibleEmployeeId));

        Assert.Empty(currentContext.TblContractAudits);
    }

    [Theory]
    [InlineData(EmployeeType.Sale)]
    [InlineData(EmployeeType.Marketing)]
    [InlineData(EmployeeType.AdminOfficer)]
    [InlineData(EmployeeType.Technical)]
    [InlineData(EmployeeType.Accountant)]
    [InlineData(EmployeeType.Manager)]
    public async Task EveryEmployeeType_CanBeTransferTarget(
        EmployeeType employeeType)
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            employeeType);

        var response = await CreateService(context)
            .TransferResponsibilityAsync(
                ContractId,
                CreateTransferRequest(),
                CurrentResponsibleEmployeeId);

        Assert.Equal(
            NewResponsibleEmployeeId,
            response.ResponsibleEmployeeId);
    }

    [Fact]
    public async Task SelfTransfer_ShouldNotUpdateOrCreateAudit()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);

        var request = CreateTransferRequest(
            CurrentResponsibleEmployeeId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    request,
                    CurrentResponsibleEmployeeId));

        var contract = await context.TblContracts.SingleAsync();

        Assert.Equal(
            CurrentResponsibleEmployeeId,
            contract.EmployeeId);
        Assert.Null(contract.UpdatedEmployeeId);
        Assert.Equal(
            InitialRowVersion(),
            contract.RowVersion);
        Assert.Empty(context.TblContractAudits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyReason_ShouldBeRejected(string reason)
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);

        var request = CreateTransferRequest();
        request.Reason = reason;

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    request,
                    CurrentResponsibleEmployeeId));

        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task NormalizedReasonAtMaximumLength_ShouldBeAccepted()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);

        var request = CreateTransferRequest();
        request.Reason = $" {new string('a', 1000)} ";
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);

        await CreateService(context).TransferResponsibilityAsync(
            ContractId,
            request,
            CurrentResponsibleEmployeeId);

        var audit = await context.TblContractAudits
            .AsNoTracking()
            .SingleAsync();

        Assert.NotNull(audit.Reason);
        Assert.Equal(1000, audit.Reason.Length);
    }

    [Fact]
    public async Task NormalizedReasonOverMaximumLength_ShouldBeRejected()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);

        var request = CreateTransferRequest();
        request.Reason = $" {new string('a', 1001)} ";

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    request,
                    CurrentResponsibleEmployeeId));

        Assert.Empty(context.TblContractAudits);
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("AQIDBAUGBw==")]
    public async Task InvalidRowVersion_ShouldBeRejected(string rowVersion)
    {
        await using var context = CreateContext();
        var request = CreateTransferRequest();
        request.RowVersion = rowVersion;

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    request,
                    CurrentResponsibleEmployeeId));

        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task AuditStagingFailure_ShouldLeaveResponsibilityUnchanged()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);

        var service = CreateService(
            context,
            writerDecorator:
                writer => new ThrowAfterStagingAuditWriter(writer));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TransferResponsibilityAsync(
                ContractId,
                CreateTransferRequest(),
                CurrentResponsibleEmployeeId));

        Assert.Empty(context.ChangeTracker.Entries());

        var contract = await context.TblContracts
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(
            CurrentResponsibleEmployeeId,
            contract.EmployeeId);
        Assert.Null(contract.UpdatedEmployeeId);
        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task StaleTransfer_ShouldKeepLatestResponsibleAndAuditConflict()
    {
        await using var context = CreateContext();
        await SeedContractAsync(
            context,
            responsibleEmployeeId: NewResponsibleEmployeeId,
            rowVersion: NewerRowVersion());
        await AddEmployeeAsync(
            context,
            OtherEmployeeId,
            EmployeeType.Marketing);
        await AddEmployeeAsync(
            context,
            ManagerEmployeeId,
            EmployeeType.Manager);

        var request = CreateTransferRequest(
            OtherEmployeeId,
            InitialRowVersion());

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    request,
                    ManagerEmployeeId));

        var contract = await context.TblContracts
            .AsNoTracking()
            .SingleAsync();
        var audit = await context.TblContractAudits
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(NewResponsibleEmployeeId, contract.EmployeeId);
        AssertTransferAudit(
            audit,
            ManagerEmployeeId,
            expectedPreviousResponsibleEmployeeId: null,
            expectedNewResponsibleEmployeeId: null,
            ContractAuditResults.ConcurrencyConflict,
            expectedReason: null);
    }

    [Fact]
    public async Task UnauthorizedStaleTransfer_ShouldNotExposeConcurrencyOrAudit()
    {
        await using var context = CreateContext();
        await SeedContractAsync(
            context,
            responsibleEmployeeId: NewResponsibleEmployeeId,
            rowVersion: NewerRowVersion());
        await AddEmployeeAsync(
            context,
            OtherEmployeeId,
            EmployeeType.Marketing);

        var request = CreateTransferRequest(
            OtherEmployeeId,
            InitialRowVersion());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateService(context)
                .TransferResponsibilityAsync(
                    ContractId,
                    request,
                    CurrentResponsibleEmployeeId));

        var contract = await context.TblContracts
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(NewResponsibleEmployeeId, contract.EmployeeId);
        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task Transfer_ShouldApplyAuthorizationImmediately()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            NewResponsibleEmployeeId,
            EmployeeType.Technical);

        var service = CreateService(context);

        await service.TransferResponsibilityAsync(
            ContractId,
            CreateTransferRequest(),
            CurrentResponsibleEmployeeId);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateDraftAsync(
                ContractId,
                CreateUpdateRequest(),
                CurrentResponsibleEmployeeId));

        await service.UpdateDraftAsync(
            ContractId,
            CreateUpdateRequest(),
            NewResponsibleEmployeeId);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.StartNegotiationAsync(
                ContractId,
                CreateNegotiationRequest(),
                CurrentResponsibleEmployeeId));

        var detail = await service.StartNegotiationAsync(
            ContractId,
            CreateNegotiationRequest(),
            NewResponsibleEmployeeId);

        Assert.Equal(ContractStatus.Negotiating, detail.Status);
    }

    [Fact]
    public async Task UpdateDraft_ShouldAuditCustomerAndEditableContentChanges()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);

        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = NewCustomerId,
            CustomerCode = "KH-NEW",
            CustomerCompany = "Công ty khách hàng mới",
            Status = 1
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var request = CreateUpdateRequest();
        request.CustomerId = NewCustomerId;
        request.ContractNameEn = "Updated contract";
        request.EffectiveDate = new DateTime(2026, 9, 1);
        request.ExpireDate = new DateTime(2027, 8, 31);
        request.Items[0].ItemCode = "SP-NEW";
        request.Items[0].DiscountMode =
            ContractItemDiscountMode.Percentage;
        request.Items[0].DiscountPercent = 10m;
        request.Items[0].VatPercent = 10m;

        await CreateService(context).UpdateDraftAsync(
            ContractId,
            request,
            CurrentResponsibleEmployeeId);

        var audit = await context.TblContractAudits
            .SingleAsync(x =>
                x.ActionType == ContractAuditActionTypes.DraftUpdated);
        using var previousDocument = JsonDocument.Parse(
            audit.PreviousValuesJson!);
        using var newDocument = JsonDocument.Parse(
            audit.NewValuesJson!);
        var previousValues = previousDocument.RootElement;
        var newValues = newDocument.RootElement;

        Assert.Equal(
            CustomerId,
            previousValues.GetProperty("CustomerId").GetInt32());
        Assert.Equal(
            "Khách hàng kiểm thử",
            previousValues.GetProperty("CustomerName").GetString());
        Assert.Equal(
            NewCustomerId,
            newValues.GetProperty("CustomerId").GetInt32());
        Assert.Equal(
            "Công ty khách hàng mới",
            newValues.GetProperty("CustomerName").GetString());
        Assert.Equal(
            "Updated contract",
            newValues.GetProperty("ContractNameEn").GetString());
        Assert.Equal(
            100m,
            newValues.GetProperty("Subtotal").GetDecimal());
        Assert.Equal(
            10m,
            newValues.GetProperty("TotalDiscount").GetDecimal());
        Assert.Equal(
            9m,
            newValues.GetProperty("TotalVat").GetDecimal());
        Assert.Contains(
            "SP-NEW",
            newValues.GetProperty("AddedItems").GetString());
        Assert.Contains(
            "GENERAL",
            newValues.GetProperty("AddedTerms").GetString());
    }

    [Fact]
    public async Task UpdateDraft_ShouldAuditUpdatedAndRemovedItemsAndTerms()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var now = DateTime.UtcNow;

        context.TblContractItems.AddRange(
            new TblContractItem
            {
                ContractItemId = 21,
                ContractId = ContractId,
                VersionId = VersionId,
                ItemType = (byte)ContractItemType.Product,
                ItemCode = "SP-OLD",
                ItemName = "Sản phẩm cũ",
                Quantity = 1m,
                UnitPrice = 100m,
                LineSubtotal = 100m,
                LineTotal = 100m,
                DisplayOrder = 1,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = now,
                RowVersion = InitialRowVersion()
            },
            new TblContractItem
            {
                ContractItemId = 22,
                ContractId = ContractId,
                VersionId = VersionId,
                ItemType = (byte)ContractItemType.Service,
                ItemCode = "DV-REMOVE",
                ItemName = "Dịch vụ sẽ xóa",
                Quantity = 1m,
                UnitPrice = 0m,
                DisplayOrder = 2,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = now,
                RowVersion = InitialRowVersion()
            });
        context.TblContractTerms.AddRange(
            new TblContractTerm
            {
                TermId = 31,
                ContractId = ContractId,
                VersionId = VersionId,
                TermCode = "GENERAL",
                TermTitle = "Điều khoản cũ",
                TermContent = "Nội dung cũ",
                IsNegotiable = true,
                DisplayOrder = 1,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = now,
                RowVersion = InitialRowVersion()
            },
            new TblContractTerm
            {
                TermId = 32,
                ContractId = ContractId,
                VersionId = VersionId,
                TermCode = "REMOVE",
                TermTitle = "Điều khoản sẽ xóa",
                DisplayOrder = 2,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = now,
                RowVersion = InitialRowVersion()
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var request = CreateUpdateRequest();
        request.Items[0].ContractItemId = 21;
        request.Items[0].RowVersion =
            Convert.ToBase64String(InitialRowVersion());
        request.Items[0].ItemCode = "SP-UPDATED";
        request.Items[0].ItemName = "Sản phẩm đã sửa";
        request.Items[0].Quantity = 2m;
        request.Terms[0].TermId = 31;
        request.Terms[0].RowVersion =
            Convert.ToBase64String(InitialRowVersion());
        request.Terms[0].TermTitle = "Điều khoản đã sửa";
        request.Terms[0].TermContent = "Nội dung mới";

        await CreateService(context).UpdateDraftAsync(
            ContractId,
            request,
            CurrentResponsibleEmployeeId);

        var audit = await context.TblContractAudits
            .SingleAsync(x =>
                x.ActionType == ContractAuditActionTypes.DraftUpdated);
        using var previousDocument = JsonDocument.Parse(
            audit.PreviousValuesJson!);
        using var newDocument = JsonDocument.Parse(
            audit.NewValuesJson!);
        var previousValues = previousDocument.RootElement;
        var newValues = newDocument.RootElement;

        Assert.Contains(
            "DV-REMOVE",
            previousValues.GetProperty("RemovedItems").GetString());
        Assert.Contains(
            "REMOVE",
            previousValues.GetProperty("RemovedTerms").GetString());
        Assert.Contains(
            "SP-UPDATED",
            newValues.GetProperty("UpdatedItems").GetString());
        Assert.Contains(
            "Số lượng",
            newValues.GetProperty("UpdatedItems").GetString());
        Assert.Contains(
            "GENERAL",
            newValues.GetProperty("UpdatedTerms").GetString());
        Assert.Contains(
            "Nội dung",
            newValues.GetProperty("UpdatedTerms").GetString());
    }

    [Fact]
    public async Task SubmitForApproval_ShouldAuditStatusLockAndApprovalRequest()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        await AddEmployeeAsync(
            context,
            ManagerEmployeeId,
            EmployeeType.Manager);

        var contract = await context.TblContracts.SingleAsync();
        contract.Status = (byte)ContractStatus.Negotiating;
        context.TblContractItems.Add(new TblContractItem
        {
            ContractItemId = 41,
            ContractId = ContractId,
            VersionId = VersionId,
            ItemType = (byte)ContractItemType.Product,
            ItemName = "Sản phẩm gửi duyệt",
            Quantity = 1m,
            UnitPrice = 100m,
            LineSubtotal = 100m,
            LineTotal = 100m,
            DisplayOrder = 1,
            CreatedEmployeeId = CreatorEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractTerms.Add(new TblContractTerm
        {
            TermId = 42,
            ContractId = ContractId,
            VersionId = VersionId,
            TermCode = "APPROVAL",
            TermTitle = "Điều khoản gửi duyệt",
            IsNegotiable = true,
            DisplayOrder = 1,
            CreatedEmployeeId = CreatorEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var response = await CreateService(context).SubmitForApprovalAsync(
            ContractId,
            new SubmitContractForApprovalRequest
            {
                RowVersion = Convert.ToBase64String(InitialRowVersion()),
                CurrentVersionId = VersionId,
                CurrentVersionRowVersion =
                    Convert.ToBase64String(InitialRowVersion())
            },
            CurrentResponsibleEmployeeId);

        var audit = await context.TblContractAudits.SingleAsync(x =>
            x.ActionType == ContractAuditActionTypes.ApprovalSubmitted);
        Assert.Equal(
            (byte)ContractStatus.Negotiating,
            audit.PreviousContractStatus);
        Assert.Equal(
            (byte)ContractStatus.PendingApproval,
            audit.NewContractStatus);

        using var previousDocument = JsonDocument.Parse(
            audit.PreviousValuesJson!);
        using var newDocument = JsonDocument.Parse(
            audit.NewValuesJson!);
        Assert.False(previousDocument.RootElement
            .GetProperty("VersionLocked").GetBoolean());
        Assert.True(newDocument.RootElement
            .GetProperty("VersionLocked").GetBoolean());
        Assert.Equal(
            response.ApprovalRequestId,
            newDocument.RootElement
                .GetProperty("ApprovalRequestId").GetInt32());
        Assert.Equal(
            (byte)ApprovalRequestStatus.Pending,
            newDocument.RootElement
                .GetProperty("ApprovalStatus").GetByte());
        Assert.Equal(
            response.SnapshotHash,
            newDocument.RootElement
                .GetProperty("SnapshotHash").GetString());
    }

    private static DbDtctechContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<DbDtctechContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString(),
                    databaseOptions =>
                        databaseOptions.EnableNullChecks(false))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(
                        InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        return new DbDtctechContext(options);
    }

    private static ContractService CreateService(
        DbDtctechContext context,
        Func<IContractAuditWriter, IContractAuditWriter>?
            writerDecorator = null)
    {
        var currentTenant = new CurrentTenant();
        currentTenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-601",
            "Tenant 601",
            TenantDatabaseMode.Dedicated,
            "InMemory"));

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = CorrelationId
        };
        httpContext.Connection.RemoteIpAddress =
            IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent =
            "ContractManagement.Tests";

        IContractAuditWriter writer = new ContractAuditWriter(
            context,
            currentTenant,
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        if (writerDecorator != null)
        {
            writer = writerDecorator(writer);
        }

        return new ContractService(
            context,
            writer,
            currentTenant,
            new StaticSubmissionRenderer(),
            new MemoryPrivateFileStorage());
    }

    private static async Task SeedContractAsync(
        DbDtctechContext context,
        int responsibleEmployeeId =
            CurrentResponsibleEmployeeId,
        byte[]? rowVersion = null)
    {
        await AddEmployeeAsync(
            context,
            CurrentResponsibleEmployeeId,
            EmployeeType.Sale);

        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Khách hàng kiểm thử",
            Status = 1
        });

        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = CustomerId,
            EmployeeId = responsibleEmployeeId,
            ContractType = (byte)ContractType.SoftwareSupply,
            CurrentVersionId = VersionId,
            ContractCode = "HD-TEST-11",
            ContractName = "Hợp đồng kiểm thử",
            Status = (byte)ContractStatus.Draft,
            TotalAmount = 100m,
            CurrencyCode = "VND",
            LanguageMode =
                (byte)ContractLanguageMode.Vietnamese,
            IsLegacy = false,
            CreatedEmployeeId = CreatorEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion ?? InitialRowVersion()
        });

        context.TblContractVersions.Add(
            new TblContractVersion
            {
                VersionId = VersionId,
                ContractId = ContractId,
                VersionNo = 1,
                IsLocked = false,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task AddEmployeeAsync(
        DbDtctechContext context,
        int employeeId,
        EmployeeType employeeType,
        byte status = 1)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = employeeId,
            EmployeeAccount = $"employee-{employeeId}",
            EmployeeFullName = $"Employee {employeeId}",
            Status = status,
            EmployeeType = (byte)employeeType
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private sealed class StaticSubmissionRenderer
        : IContractSubmissionArtifactRenderer
    {
        public Task<ContractSubmissionArtifactRenderResult> RenderAsync(
            int contractId,
            int employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ContractSubmissionArtifactRenderResult(
                "{\"schemaVersion\":4}",
                4,
                7001,
                [0x50, 0x4B, 0x03, 0x04, 0x01],
                "contract-submitted.docx",
                "%PDF-test"u8.ToArray(),
                "contract-submitted.pdf"));
    }

    private sealed class MemoryPrivateFileStorage : IPrivateFileStorage
    {
        public async Task<StoredPrivateFile> SaveAsync(
            PrivateFileSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            await using var memory = new MemoryStream();
            await request.Content.CopyToAsync(memory, cancellationToken);
            var content = memory.ToArray();
            return new StoredPrivateFile(
                $"{request.TenantCode}/{request.ObjectType}/{request.ObjectId}/{Guid.NewGuid():N}{Path.GetExtension(request.OriginalFileName)}",
                request.OriginalFileName,
                request.ContentType,
                content.LongLength,
                Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(),
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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static TransferContractResponsibilityRequest
        CreateTransferRequest(
            int newResponsibleEmployeeId =
                NewResponsibleEmployeeId,
            byte[]? rowVersion = null)
    {
        return new TransferContractResponsibilityRequest
        {
            NewResponsibleEmployeeId =
                newResponsibleEmployeeId,
            Reason = "  Bàn giao phụ trách  ",
            RowVersion = Convert.ToBase64String(
                rowVersion ?? InitialRowVersion())
        };
    }

    private static UpdateContractDraftRequest
        CreateUpdateRequest()
    {
        return new UpdateContractDraftRequest
        {
            RowVersion =
                Convert.ToBase64String(InitialRowVersion()),
            CurrentVersionId = VersionId,
            CurrentVersionRowVersion =
                Convert.ToBase64String(InitialRowVersion()),
            CustomerId = CustomerId,
            ContractName = "Hợp đồng đã cập nhật",
            CurrencyCode = "VND",
            Items =
            [
                new UpdateContractItemRequest
                {
                    ItemType = ContractItemType.Product,
                    ItemName = "Sản phẩm kiểm thử",
                    Quantity = 1m,
                    UnitPrice = 100m,
                    DisplayOrder = 1
                }
            ],
            Terms =
            [
                new UpdateContractTermRequest
                {
                    TermCode = "GENERAL",
                    TermTitle = "Điều khoản chung",
                    TermContent = "Nội dung kiểm thử",
                    IsNegotiable = true,
                    DisplayOrder = 1
                }
            ]
        };
    }

    private static StartContractNegotiationRequest
        CreateNegotiationRequest()
    {
        return new StartContractNegotiationRequest
        {
            RowVersion =
                Convert.ToBase64String(InitialRowVersion())
        };
    }

    private static void AssertTransferAudit(
        TblContractAudit audit,
        int expectedActorEmployeeId,
        int? expectedPreviousResponsibleEmployeeId,
        int? expectedNewResponsibleEmployeeId,
        string expectedResult,
        string? expectedReason)
    {
        Assert.Equal(TenantId, audit.TenantId);
        Assert.Equal(ContractId, audit.ContractId);
        Assert.Equal(
            ContractAuditActorTypes.Employee,
            audit.ActorType);
        Assert.Equal(
            expectedActorEmployeeId,
            audit.ActorEmployeeId);
        Assert.Equal(
            ContractAuditActionTypes.ResponsibilityTransferred,
            audit.ActionType);
        Assert.Equal(expectedResult, audit.Result);
        Assert.Equal(
            expectedPreviousResponsibleEmployeeId,
            audit.PreviousResponsibleEmployeeId);
        Assert.Equal(
            expectedNewResponsibleEmployeeId,
            audit.NewResponsibleEmployeeId);
        Assert.Equal(expectedReason, audit.Reason);
        Assert.Equal(CorrelationId, audit.CorrelationId);
        Assert.Equal(DateTimeKind.Utc, audit.OccurredAt.Kind);
    }

    private static byte[] InitialRowVersion() =>
        [1, 2, 3, 4, 5, 6, 7, 8];

    private static byte[] NewerRowVersion() =>
        [8, 7, 6, 5, 4, 3, 2, 1];

    private sealed class ThrowAfterStagingAuditWriter(
        IContractAuditWriter inner) : IContractAuditWriter
    {
        public void StageAudits(
            IReadOnlyCollection<ContractAuditWriteRequest> requests) =>
            inner.StageAudits(requests);

        public void StageEmployeeAudits(
            IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests)
        {
            inner.StageEmployeeAudits(requests);

            throw new InvalidOperationException(
                "Simulated audit staging failure.");
        }
    }
}
