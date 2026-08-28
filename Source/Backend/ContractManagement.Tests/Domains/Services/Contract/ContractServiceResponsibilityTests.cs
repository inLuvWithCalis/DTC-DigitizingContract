using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Net;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ContractManagement.Tests.Domains.Services.Contract;

public class ContractServiceResponsibilityTests
{
    private const int CreatorEmployeeId = 101;
    private const int ResponsibleEmployeeId = 202;
    private const int CustomerId = 301;
    private const int TemplateId = 401;
    private const int TemplateVersionId = 402;
    private const int TenantId = 601;
    private const string CorrelationId = "slice-02-test-correlation";

    [Fact]
    public async Task CreateAsync_ShouldDefaultResponsibilityToCreator()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var service = CreateService(context);
        var response = await service.CreateAsync(
            CreateRequest(),
            CreatorEmployeeId);

        var contract = await context.TblContracts.SingleAsync();
        var audits = await context.TblContractAudits
            .OrderBy(audit => audit.ContractAuditId)
            .ToListAsync();

        Assert.Equal(CreatorEmployeeId, response.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.CreatedEmployeeId);
        Assert.Collection(
            audits,
            audit => AssertCreateAudit(
                audit,
                ContractAuditActionTypes.ContractCreated,
                contract,
                CreatorEmployeeId),
            audit => AssertCreateAudit(
                audit,
                ContractAuditActionTypes.ResponsibleAssigned,
                contract,
                CreatorEmployeeId));
        Assert.Null(audits[1].PreviousResponsibleEmployeeId);
        Assert.Null(audits[1].Reason);

        using var createdValuesDocument = JsonDocument.Parse(
            audits[0].NewValuesJson!);
        var createdValues = createdValuesDocument.RootElement;
        Assert.Equal(
            (byte)ContractType.SoftwareSupply,
            createdValues.GetProperty("ContractType").GetByte());
        Assert.Equal(
            (byte)ContractLanguageMode.Vietnamese,
            createdValues.GetProperty("LanguageMode").GetByte());
        Assert.Equal(
            TemplateVersionId,
            createdValues.GetProperty("TemplateVersionId").GetInt32());
        Assert.Contains(
            "Sản phẩm kiểm thử",
            createdValues.GetProperty("AddedItems").GetString());
        Assert.Contains(
            "GENERAL",
            createdValues.GetProperty("AddedTerms").GetString());
    }

    [Fact]
    public async Task CreateAsync_ShouldUseSelectedActiveEmployee()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await AddEmployeeAsync(
            context,
            ResponsibleEmployeeId,
            status: 1,
            EmployeeType.Technical);

        var service = CreateService(context);
        var response = await service.CreateAsync(
            CreateRequest(ResponsibleEmployeeId),
            CreatorEmployeeId);

        var contract = await context.TblContracts.SingleAsync();
        var audits = await context.TblContractAudits
            .OrderBy(audit => audit.ContractAuditId)
            .ToListAsync();

        Assert.Equal(ResponsibleEmployeeId, response.EmployeeId);
        Assert.Equal(ResponsibleEmployeeId, contract.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.CreatedEmployeeId);
        Assert.Collection(
            audits,
            audit => AssertCreateAudit(
                audit,
                ContractAuditActionTypes.ContractCreated,
                contract,
                ResponsibleEmployeeId),
            audit => AssertCreateAudit(
                audit,
                ContractAuditActionTypes.ResponsibleAssigned,
                contract,
                ResponsibleEmployeeId));
        Assert.All(
            audits,
            audit => Assert.Equal(
                CreatorEmployeeId,
                audit.ActorEmployeeId));
        Assert.Null(audits[1].PreviousResponsibleEmployeeId);
        Assert.Null(audits[1].Reason);
        Assert.Equal(
            audits[0].CorrelationId,
            audits[1].CorrelationId);
        Assert.Equal(
            audits[0].OccurredAt,
            audits[1].OccurredAt);
    }

    [Fact]
    public async Task CreateAsync_UsesOnlyCurrentPublishedTemplateVersion_AndRejectsItAfterRetire()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        const int replacementVersionId = TemplateVersionId + 1;
        context.TblContractTemplateVersions.Add(new TblContractTemplateVersion
        {
            TemplateVersionId = replacementVersionId,
            TemplateId = TemplateId,
            VersionNo = 2,
            Status = (byte)TemplateVersionStatus.Published,
            ValidationStatus = (byte)TemplateValidationStatus.Valid,
            CreatedEmployeeId = CreatorEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = []
        });
        context.TblContractTemplateTerms.Add(new TblContractTemplateTerm
        {
            TemplateVersionId = replacementVersionId,
            TermCode = "GENERAL",
            TermTitle = "Replacement term",
            TermContent = "Replacement published template term.",
            IsNegotiable = true,
            DisplayOrder = 1,
            CreatedEmployeeId = CreatorEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = []
        });
        var template = await context.TblContractTemplates.SingleAsync();
        template.CurrentPublishedVersionId = replacementVersionId;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = CreateRequest();
        request.TemplateVersionId = replacementVersionId;
        var created = await service.CreateAsync(request, CreatorEmployeeId);
        Assert.Equal(replacementVersionId, created.TemplateVersionId);

        var replacement = await context.TblContractTemplateVersions
            .SingleAsync(version => version.TemplateVersionId == replacementVersionId);
        replacement.Status = (byte)TemplateVersionStatus.Retired;
        template.CurrentPublishedVersionId = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(request, CreatorEmployeeId));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingResponsibleEmployee()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                CreateRequest(ResponsibleEmployeeId),
                CreatorEmployeeId));

        Assert.Empty(context.TblContracts);
        Assert.Empty(context.TblContractAudits);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactiveResponsibleEmployee()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await AddEmployeeAsync(
            context,
            ResponsibleEmployeeId,
            status: 0,
            EmployeeType.Marketing);

        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(
                CreateRequest(ResponsibleEmployeeId),
                CreatorEmployeeId));

        Assert.Empty(context.TblContracts);
        Assert.Empty(context.TblContractAudits);
    }

    [Theory]
    [InlineData(EmployeeType.Sale)]
    [InlineData(EmployeeType.Marketing)]
    [InlineData(EmployeeType.AdminOfficer)]
    [InlineData(EmployeeType.Technical)]
    [InlineData(EmployeeType.Accountant)]
    [InlineData(EmployeeType.Manager)]
    public async Task CreateAsync_ShouldAllowEveryEmployeeType(
        EmployeeType employeeType)
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await AddEmployeeAsync(
            context,
            ResponsibleEmployeeId,
            status: 1,
            employeeType);

        var service = CreateService(context);
        var response = await service.CreateAsync(
            CreateRequest(ResponsibleEmployeeId),
            CreatorEmployeeId);

        Assert.Equal(ResponsibleEmployeeId, response.EmployeeId);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotResolveEmployeeFromAnotherTenantContext()
    {
        await using var currentTenantContext = CreateContext();
        await SeedCreateDependenciesAsync(currentTenantContext);

        await using var otherTenantContext = CreateContext();
        await AddEmployeeAsync(
            otherTenantContext,
            ResponsibleEmployeeId,
            status: 1,
            EmployeeType.Manager);

        var service = CreateService(currentTenantContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                CreateRequest(ResponsibleEmployeeId),
                CreatorEmployeeId));

        Assert.Empty(currentTenantContext.TblContracts);
        Assert.Empty(currentTenantContext.TblContractAudits);
    }

    [Fact]
    public async Task ContractAudit_ShouldKeepTenantDatabasesIsolated()
    {
        await using var firstContext = CreateContext();
        await SeedCreateDependenciesAsync(firstContext);

        await using var secondContext = CreateContext();
        await SeedCreateDependenciesAsync(secondContext);

        await CreateService(firstContext, tenantId: 701)
            .CreateAsync(CreateRequest(), CreatorEmployeeId);
        await CreateService(secondContext, tenantId: 702)
            .CreateAsync(CreateRequest(), CreatorEmployeeId);

        var firstAudits =
            await firstContext.TblContractAudits.ToListAsync();
        var secondAudits =
            await secondContext.TblContractAudits.ToListAsync();

        Assert.Equal(2, firstAudits.Count);
        Assert.Equal(2, secondAudits.Count);
        Assert.All(
            firstAudits,
            audit => Assert.Equal(701, audit.TenantId));
        Assert.All(
            secondAudits,
            audit => Assert.Equal(702, audit.TenantId));
    }

    [Fact]
    public async Task CreateAsync_ShouldClearFailedAttemptBeforeNextAttempt()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var service = CreateService(
            context,
            auditWriterDecorator:
                writer => new ThrowOnceAfterStagingAuditWriter(writer));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(
                CreateRequest(),
                CreatorEmployeeId));

        Assert.Empty(context.ChangeTracker.Entries());

        /*
         * EF Core InMemory bỏ qua transaction. Xóa các identity rows đã được
         * lưu trước lỗi để mô phỏng database rollback trước attempt kế tiếp.
         * Test này chứng minh cleanup của ChangeTracker, không phải SQL retry.
         */
        context.TblContractVersions.RemoveRange(
            await context.TblContractVersions.ToListAsync());
        context.TblContracts.RemoveRange(
            await context.TblContracts.ToListAsync());
        await context.SaveChangesAsync();

        await service.CreateAsync(
            CreateRequest(),
            CreatorEmployeeId);

        var contract = await context.TblContracts.SingleAsync();
        var version = await context.TblContractVersions.SingleAsync();
        var audits = await context.TblContractAudits.ToListAsync();

        Assert.Equal(contract.ContractId, version.ContractId);
        Assert.Equal(2, audits.Count);
        Assert.All(
            audits,
            audit =>
            {
                Assert.Equal(contract.ContractId, audit.ContractId);
                Assert.Equal(version.VersionId, audit.VersionId);
            });
        Assert.Contains(
            audits,
            audit =>
                audit.ActionType ==
                ContractAuditActionTypes.ContractCreated);
        Assert.Contains(
            audits,
            audit =>
                audit.ActionType ==
                ContractAuditActionTypes.ResponsibleAssigned);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EmployeeAudit_ShouldRequirePositiveActorEmployeeId(
        int? actorEmployeeId)
    {
        await using var context = CreateContext();
        context.TblContractAudits.Add(
            CreateAudit(
                actorType: ContractAuditActorTypes.Employee,
                actorEmployeeId));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task NonEmployeeAudit_ShouldRejectActorEmployeeId()
    {
        await using var context = CreateContext();
        context.TblContractAudits.Add(
            CreateAudit(
                actorType: "Customer",
                actorEmployeeId: CreatorEmployeeId));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CustomerAudit_ShouldRequireSessionAndAllowNullActorEmployeeId()
    {
        await using var context = CreateContext();
        var audit = CreateAudit(
            actorType: "Customer",
            actorEmployeeId: null);
        audit.ActorCustomerAccessSessionId = 1;
        context.TblContractAudits.Add(audit);

        await context.SaveChangesAsync();

        var persistedAudit = await context.TblContractAudits.SingleAsync();
        Assert.Null(persistedAudit.ActorEmployeeId);
        Assert.Equal(1, persistedAudit.ActorCustomerAccessSessionId);
    }

    [Fact]
    public async Task ContractAudit_ShouldRejectModification()
    {
        await using var context = CreateContext();
        var audit = CreateAudit(
            ContractAuditActorTypes.Employee,
            CreatorEmployeeId);
        context.TblContractAudits.Add(audit);
        await context.SaveChangesAsync();

        audit.Result = "Changed";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ContractAudit_ShouldRejectDeletion()
    {
        await using var context = CreateContext();
        var audit = CreateAudit(
            ContractAuditActorTypes.Employee,
            CreatorEmployeeId);
        context.TblContractAudits.Add(audit);
        await context.SaveChangesAsync();

        context.TblContractAudits.Remove(audit);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResponsibleEmployeeId_ShouldRejectNonPositiveValue(
        int responsibleEmployeeId)
    {
        var request = CreateRequest(responsibleEmployeeId);
        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(request)
        {
            MemberName = nameof(CreateContractRequest.ResponsibleEmployeeId)
        };

        var isValid = Validator.TryValidateProperty(
            request.ResponsibleEmployeeId,
            validationContext,
            results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
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
        int tenantId = TenantId,
        Func<IContractAuditWriter, IContractAuditWriter>?
            auditWriterDecorator = null)
    {
        var currentTenant = new CurrentTenant();
        currentTenant.Set(new ResolvedTenant(
            tenantId,
            $"TENANT-{tenantId}",
            $"Tenant {tenantId}",
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

        IContractAuditWriter auditWriter = new ContractAuditWriter(
            context,
            currentTenant,
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        if (auditWriterDecorator != null)
        {
            auditWriter = auditWriterDecorator(auditWriter);
        }

        return new ContractService(context, auditWriter);
    }

    private sealed class ThrowOnceAfterStagingAuditWriter(
        IContractAuditWriter inner) : IContractAuditWriter
    {
        private bool _shouldThrow = true;

        public void StageAudits(
            IReadOnlyCollection<ContractAuditWriteRequest> requests) =>
            inner.StageAudits(requests);

        public void StageEmployeeAudits(
            IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests)
        {
            inner.StageEmployeeAudits(requests);

            if (!_shouldThrow)
            {
                return;
            }

            _shouldThrow = false;

            throw new InvalidOperationException(
                "Simulated failure after audit staging.");
        }
    }

    private static void AssertCreateAudit(
        TblContractAudit audit,
        string expectedAction,
        TblContract contract,
        int expectedResponsibleEmployeeId)
    {
        Assert.Equal(TenantId, audit.TenantId);
        Assert.Equal(contract.ContractId, audit.ContractId);
        Assert.Equal(contract.CurrentVersionId, audit.VersionId);
        Assert.Equal(
            ContractAuditActorTypes.Employee,
            audit.ActorType);
        Assert.Equal(CreatorEmployeeId, audit.ActorEmployeeId);
        Assert.Equal(expectedAction, audit.ActionType);
        Assert.Equal(ContractAuditResults.Succeeded, audit.Result);
        Assert.Null(audit.PreviousContractStatus);
        Assert.Equal(contract.Status, audit.NewContractStatus);
        Assert.Equal(
            expectedResponsibleEmployeeId,
            audit.NewResponsibleEmployeeId);
        Assert.Equal(contract.CreatedDate, audit.OccurredAt);
        Assert.Equal(CorrelationId, audit.CorrelationId);
        Assert.Equal("127.0.0.1", audit.IpAddress);
        Assert.Equal(
            "ContractManagement.Tests",
            audit.UserAgent);
    }

    private static TblContractAudit CreateAudit(
        string actorType,
        int? actorEmployeeId)
    {
        return new TblContractAudit
        {
            TenantId = TenantId,
            ContractId = 1,
            VersionId = 1,
            ActorType = actorType,
            ActorEmployeeId = actorEmployeeId,
            ActionType = ContractAuditActionTypes.ContractCreated,
            Result = ContractAuditResults.Succeeded,
            NewContractStatus = (byte)ContractStatus.Draft,
            NewResponsibleEmployeeId = CreatorEmployeeId,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = CorrelationId
        };
    }

    private static async Task SeedCreateDependenciesAsync(
        DbDtctechContext context)
    {
        await AddEmployeeAsync(
            context,
            CreatorEmployeeId,
            status: 1,
            EmployeeType.Sale);

        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Khách hàng kiểm thử",
            Status = 1
        });

        context.TblContractTemplates.Add(new TblContractTemplate
        {
            TemplateId = TemplateId,
            TemplateCode = "TEST_SOFTWARE_SUPPLY",
            TemplateName = "Template kiểm thử",
            DocumentType =
                (byte)TemplateDocumentType.SoftwareSupplyContract,
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            CurrentPublishedVersionId = TemplateVersionId,
            IsActive = true,
            CreatedEmployeeId = CreatorEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = []
        });

        context.TblContractTemplateVersions.Add(
            new TblContractTemplateVersion
            {
                TemplateVersionId = TemplateVersionId,
                TemplateId = TemplateId,
                VersionNo = 1,
                Status = (byte)TemplateVersionStatus.Published,
                ValidationStatus =
                    (byte)TemplateValidationStatus.Valid,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });

        context.TblContractTemplateTerms.Add(
            new TblContractTemplateTerm
            {
                TemplateTermId = 501,
                TemplateVersionId = TemplateVersionId,
                TermCode = "GENERAL",
                TermTitle = "Điều khoản chung",
                TermContent = "Nội dung kiểm thử",
                IsNegotiable = true,
                DisplayOrder = 1,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });

        await context.SaveChangesAsync();
    }

    private static async Task AddEmployeeAsync(
        DbDtctechContext context,
        int employeeId,
        byte status,
        EmployeeType employeeType)
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
    }

    private static CreateContractRequest CreateRequest(
        int? responsibleEmployeeId = null)
    {
        return new CreateContractRequest
        {
            CustomerId = CustomerId,
            ResponsibleEmployeeId = responsibleEmployeeId,
            ContractType = ContractType.SoftwareSupply,
            TemplateVersionId = TemplateVersionId,
            ContractName = "Contract kiểm thử",
            CurrencyCode = "VND",
            LanguageMode = ContractLanguageMode.Vietnamese,
            Items =
            [
                new CreateContractItemRequest
                {
                    ItemType = ContractItemType.Product,
                    ItemName = "Sản phẩm kiểm thử",
                    Quantity = 1m,
                    UnitPrice = 100m,
                    DisplayOrder = 1
                }
            ]
        };
    }
}
