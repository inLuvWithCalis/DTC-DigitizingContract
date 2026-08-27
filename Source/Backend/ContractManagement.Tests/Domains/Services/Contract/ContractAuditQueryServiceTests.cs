using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
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
using Xunit;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class ContractAuditQueryServiceTests
{
    private const int TenantId = 807;
    private const int ManagerId = 11;
    private const int ResponsibleId = 12;
    private const int OtherEmployeeId = 13;
    private const int ContractId = 7001;

    [Fact]
    public async Task QueryAsync_ManagerSeesOnlyCurrentTenantAndUsesStableSort()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var tenant = CreateTenant();
        var writer = CreateWriter(context, tenant);
        var occurredAt = new DateTime(2026, 8, 7, 3, 0, 0, DateTimeKind.Utc);

        writer.StageEmployeeAudits(
        [
            NewDraftAudit(ManagerId, occurredAt, "First"),
            NewDraftAudit(ManagerId, occurredAt, "Second")
        ]);
        context.TblContractAudits.Add(new TblContractAudit
        {
            TenantId = TenantId + 1,
            ContractId = ContractId,
            VersionId = 1,
            ActorType = ContractAuditActorTypes.Employee,
            ActorEmployeeId = ManagerId,
            ActionType = ContractAuditActionTypes.DraftUpdated,
            Result = ContractAuditResults.Succeeded,
            OccurredAt = occurredAt.AddMinutes(1),
            CorrelationId = "other-tenant"
        });
        await context.SaveChangesAsync();

        var service = new ContractAuditQueryService(context, tenant);
        var result = await service.QueryAsync(
            new ContractAuditFilterRequest { PageSize = 20 },
            ManagerId);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, audit => Assert.Equal(ContractId, audit.ContractId));
        Assert.True(result.Items[0].ContractAuditId > result.Items[1].ContractAuditId);
        Assert.All(result.Items, audit =>
        {
            Assert.Equal(ContractAuditSubjectTypes.Contract, audit.SubjectType);
            Assert.Equal(ContractId, audit.SubjectId);
            Assert.Equal($"Employee {ManagerId}", audit.ActorDisplayName);
            Assert.Equal("HD-7001", audit.ContractCode);
            Assert.Equal("Hợp đồng kiểm thử", audit.ContractName);
            Assert.Equal(1, audit.VersionNo);
            Assert.NotNull(audit.NewValues);
            Assert.DoesNotContain("PhoneNumber", audit.NewValues!.Keys);
        });
    }

    [Fact]
    public async Task QueryAsync_CustomerActorResolvesNameFromAccessSession()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var tenant = CreateTenant();
        const int sessionId = 91;
        var now = new DateTime(2026, 8, 7, 3, 0, 0, DateTimeKind.Utc);
        context.TblContractCustomerAccessSessions.Add(
            new TblContractCustomerAccessSession
            {
                CustomerAccessSessionId = sessionId,
                TenantId = TenantId,
                LinkId = 1,
                ContractId = ContractId,
                VersionId = 1,
                VerificationPhoneId = 1,
                SessionTokenHash = new string('a', 64),
                IssuedAt = now,
                LastActivityAt = now,
                IdleExpiresAt = now.AddHours(1),
                HardExpiresAt = now.AddHours(8)
            });
        CreateWriter(context, tenant).StageAudits(
        [
            new ContractAuditWriteRequest(
                ContractId,
                1,
                ContractAuditActorTypes.Customer,
                null,
                sessionId,
                ContractAuditActionTypes.PublicVersionViewed,
                ContractAuditResults.Succeeded,
                now,
                SubjectType: ContractAuditSubjectTypes.ContractVersion,
                SubjectId: 1)
        ]);
        await context.SaveChangesAsync();

        var result = await new ContractAuditQueryService(context, tenant)
            .QueryAsync(
                new ContractAuditFilterRequest { PageSize = 20 },
                ManagerId);

        var audit = Assert.Single(result.Items);
        Assert.Equal(sessionId, audit.ActorCustomerAccessSessionId);
        Assert.Equal("Nguyễn Văn Khách", audit.ActorDisplayName);
        Assert.Equal("*********6789", audit.ActorMaskedPhone);
        Assert.Equal("CustomerMobile", audit.ActorPhoneSource);

        var storedAudit = await context.TblContractAudits.SingleAsync();
        Assert.Equal("Nguyễn Văn Khách", storedAudit.ActorDisplayNameSnapshot);
        Assert.Equal("*********6789", storedAudit.ActorMaskedPhoneSnapshot);
        Assert.Equal("CustomerMobile", storedAudit.ActorPhoneSourceSnapshot);

        var customer = await context.TblCustomers.SingleAsync();
        var contract = await context.TblContracts.SingleAsync();
        customer.CustomerFullName = "Tên khách hàng mới";
        contract.ContractName = "Tên hợp đồng mới";
        await context.SaveChangesAsync();

        var afterProfileChange = await new ContractAuditQueryService(context, tenant)
            .QueryAsync(
                new ContractAuditFilterRequest { PageSize = 20 },
                ManagerId);
        var immutableAudit = Assert.Single(afterProfileChange.Items);
        Assert.Equal("Nguyễn Văn Khách", immutableAudit.ActorDisplayName);
        Assert.Equal("Hợp đồng kiểm thử", immutableAudit.ContractName);
    }

    [Fact]
    public async Task QueryAsync_UsesCursorWithoutDuplicateRows()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var tenant = CreateTenant();
        var writer = CreateWriter(context, tenant);
        var occurredAt = new DateTime(2026, 8, 7, 3, 0, 0, DateTimeKind.Utc);
        writer.StageEmployeeAudits(
        [
            NewDraftAudit(ManagerId, occurredAt.AddMinutes(2), "Third"),
            NewDraftAudit(ManagerId, occurredAt.AddMinutes(1), "Second"),
            NewDraftAudit(ManagerId, occurredAt, "First")
        ]);
        await context.SaveChangesAsync();

        var service = new ContractAuditQueryService(context, tenant);
        var first = await service.QueryAsync(
            new ContractAuditFilterRequest { PageSize = 2 },
            ManagerId);
        var second = await service.QueryAsync(
            new ContractAuditFilterRequest
            {
                PageSize = 2,
                Cursor = first.NextCursor
            },
            ManagerId);

        Assert.Equal(3, first.TotalCount);
        Assert.True(first.HasMore);
        Assert.NotNull(first.NextCursor);
        Assert.Equal(2, first.Items.Count);
        Assert.False(second.HasMore);
        Assert.Single(second.Items);
        Assert.Empty(first.Items.Select(x => x.ContractAuditId)
            .Intersect(second.Items.Select(x => x.ContractAuditId)));
    }

    [Fact]
    public async Task ExportCsvAsync_AppliesTraceFiltersAndIncludesBom()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var tenant = CreateTenant();
        CreateWriter(context, tenant).StageEmployeeAudits(
        [NewDraftAudit(ManagerId, DateTime.UtcNow, "Export")]);
        await context.SaveChangesAsync();

        var service = new ContractAuditQueryService(context, tenant);
        var export = await service.ExportCsvAsync(
            new ContractAuditFilterRequest
            {
                ActorEmployeeId = ManagerId,
                CorrelationId = "audit-tests",
                SubjectType = ContractAuditSubjectTypes.Contract,
                SubjectId = ContractId
            },
            ManagerId);

        Assert.StartsWith("contract-audits-", export.FileName);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, export.Content[..3]);
        var csv = System.Text.Encoding.UTF8.GetString(export.Content);
        Assert.Contains("HD-7001", csv);
        Assert.Contains($"Employee {ManagerId}", csv);
        Assert.DoesNotContain("Employee 12", csv);
    }

    [Fact]
    public async Task QueryAsync_ResponsibleMustSelectCurrentContract()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var tenant = CreateTenant();
        var writer = CreateWriter(context, tenant);
        writer.StageEmployeeAudits(
        [NewDraftAudit(ResponsibleId, DateTime.UtcNow, "Responsible")]);
        await context.SaveChangesAsync();

        var service = new ContractAuditQueryService(context, tenant);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.QueryAsync(
                new ContractAuditFilterRequest { PageSize = 20 },
                ResponsibleId));

        var result = await service.QueryAsync(
            new ContractAuditFilterRequest
            {
                ContractId = ContractId,
                PageSize = 20
            },
            ResponsibleId);
        Assert.Single(result.Items);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.QueryAsync(
                new ContractAuditFilterRequest
                {
                    ContractId = ContractId,
                    PageSize = 20
                },
                OtherEmployeeId));
    }

    [Fact]
    public async Task Writer_RejectsSensitiveValuesAndSanitizesReason()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var writer = CreateWriter(context, CreateTenant());

        Assert.Throws<InvalidOperationException>(() => writer.StageEmployeeAudits(
        [
            new EmployeeContractAuditWriteRequest(
                ContractId,
                1,
                ManagerId,
                ContractAuditActionTypes.DraftUpdated,
                ContractAuditResults.Succeeded,
                DateTime.UtcNow,
                NewValues: ContractAuditValues.Create(
                    ("PhoneNumber", "+84912345678")))
        ]));

        writer.StageEmployeeAudits(
        [
            new EmployeeContractAuditWriteRequest(
                ContractId,
                1,
                ManagerId,
                ContractAuditActionTypes.ResponsibilityTransferred,
                ContractAuditResults.Succeeded,
                DateTime.UtcNow,
                Reason: "Call +84912345678 then open https://example.test/private",
                SubjectType: ContractAuditSubjectTypes.Contract,
                SubjectId: ContractId,
                NewValues: ContractAuditValues.Create(
                    ("Status", (byte)ContractStatus.Draft),
                    ("ResponsibleEmployeeId", ResponsibleId)))
        ]);
        await context.SaveChangesAsync();

        var audit = await context.TblContractAudits.SingleAsync();
        Assert.DoesNotContain("84912345678", audit.Reason);
        Assert.DoesNotContain("example.test", audit.Reason);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options =>
                options.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedAsync(DbDtctechContext context)
    {
        context.TblEmployees.AddRange(
            NewEmployee(ManagerId, EmployeeType.Manager),
            NewEmployee(ResponsibleId, EmployeeType.Sale),
            NewEmployee(OtherEmployeeId, EmployeeType.Sale));
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = 501,
            CustomerCode = "KH-501",
            CustomerFullName = "Nguyễn Văn Khách",
            CustomerCompany = "Công ty Khách hàng"
        });
        context.TblContractCustomerVerificationPhones.Add(
            new TblContractCustomerVerificationPhone
            {
                VerificationPhoneId = 1,
                ContractId = ContractId,
                PhoneSource = "CustomerMobile",
                PhoneNumberNormalized = "+849123456789",
                Reason = "Test",
                CreatedByEmployeeId = ManagerId,
                CreatedDate = DateTime.UtcNow
            });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = 501,
            EmployeeId = ResponsibleId,
            ContractCode = "HD-7001",
            ContractName = "Hợp đồng kiểm thử",
            Status = (byte)ContractStatus.Draft
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = 1,
            ContractId = ContractId,
            VersionNo = 1,
            CreatedEmployeeId = ManagerId,
            CreatedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static TblEmployee NewEmployee(int employeeId, EmployeeType type) => new()
    {
        EmployeeId = employeeId,
        EmployeeAccount = $"employee-{employeeId}",
        EmployeeFullName = $"Employee {employeeId}",
        EmployeeType = (byte)type,
        Status = 1
    };

    private static CurrentTenant CreateTenant()
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "AUDIT-807",
            "Audit tenant",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        return tenant;
    }

    private static IContractAuditWriter CreateWriter(
        DbDtctechContext context,
        CurrentTenant tenant)
    {
        var httpContext = new DefaultHttpContext { TraceIdentifier = "audit-tests" };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "ContractManagement.Tests";
        return new ContractAuditWriter(
            context,
            tenant,
            new HttpContextAccessor { HttpContext = httpContext });
    }

    private static EmployeeContractAuditWriteRequest NewDraftAudit(
        int employeeId,
        DateTime occurredAt,
        string contractName) => new(
        ContractId,
        1,
        employeeId,
        ContractAuditActionTypes.DraftUpdated,
        ContractAuditResults.Succeeded,
        occurredAt,
        SubjectType: ContractAuditSubjectTypes.Contract,
        SubjectId: ContractId,
        NewValues: ContractAuditValues.Create(
            ("Status", (byte)ContractStatus.Draft),
            ("ResponsibleEmployeeId", ResponsibleId),
            ("CurrentVersionId", 1),
            ("ContractName", contractName),
            ("ItemCount", 1),
            ("TermCount", 1)));
}
