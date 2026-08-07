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
            new ContractAuditFilterRequest { Page = 1, PageSize = 20 },
            ManagerId);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, audit => Assert.Equal(ContractId, audit.ContractId));
        Assert.True(result.Items[0].ContractAuditId > result.Items[1].ContractAuditId);
        Assert.All(result.Items, audit =>
        {
            Assert.Equal(ContractAuditSubjectTypes.Contract, audit.SubjectType);
            Assert.Equal(ContractId, audit.SubjectId);
            Assert.NotNull(audit.NewValues);
            Assert.DoesNotContain("PhoneNumber", audit.NewValues!.Keys);
        });
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
                new ContractAuditFilterRequest { Page = 1, PageSize = 20 },
                ResponsibleId));

        var result = await service.QueryAsync(
            new ContractAuditFilterRequest
            {
                ContractId = ContractId,
                Page = 1,
                PageSize = 20
            },
            ResponsibleId);
        Assert.Single(result.Items);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.QueryAsync(
                new ContractAuditFilterRequest
                {
                    ContractId = ContractId,
                    Page = 1,
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
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            EmployeeId = ResponsibleId,
            Status = (byte)ContractStatus.Draft
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
