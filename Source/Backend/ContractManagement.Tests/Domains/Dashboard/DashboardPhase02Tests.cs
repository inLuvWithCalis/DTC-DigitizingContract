using System.Text.Json;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.AdminDashboard;
using ContractManagement.API.Domains.DTOs.Requests.Dashboard;
using ContractManagement.API.Domains.Services.Admin;
using ContractManagement.API.Domains.Services.Dashboard;
using ContractManagement.API.Domains.CustomerAccess;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace ContractManagement.Tests.Domains.Dashboard;

public sealed class DashboardPhase02Tests
{
    [Fact]
    public async Task EmployeeDashboard_ScopesRowsAndAmountsToResponsibleEmployee()
    {
        await using var context = CreateTenantContext();
        var now = DateTime.UtcNow;
        context.TblEmployees.AddRange(
            Employee(1, EmployeeType.Sale),
            Employee(2, EmployeeType.Sale));
        context.TblContracts.AddRange(
            Contract(101, 1, ContractStatus.Draft, 100, "VND", now),
            Contract(102, 1, ContractStatus.Signed, 5, "USD", now, now.AddDays(5)),
            Contract(201, 2, ContractStatus.PendingApproval, 999, "VND", now));
        context.TblContractAudits.AddRange(
            Audit(1, 101, 1, now, "Sale A"),
            Audit(2, 201, 2, now, "Sale B"));
        await context.SaveChangesAsync();

        var result = await new DashboardService(context).GetAsync(
            1,
            new DashboardFilterRequest
            {
                From = now.AddDays(-1),
                To = now.AddDays(1),
                ExpiryDays = 30
            });

        Assert.Equal("Own", result.Scope);
        Assert.Equal(2, result.Summary.Single(item => item.Key == "total").Count);
        Assert.DoesNotContain(result.RecentActivities, item => item.ContractId == 201);
        Assert.Equal(100, result.AmountByCurrency.Single(item => item.Currency == "VND").Amount);
        Assert.Equal(5, result.AmountByCurrency.Single(item => item.Currency == "USD").Amount);
        Assert.Single(result.ExpiringContracts);
    }

    [Fact]
    public async Task ManagerDashboard_UsesTenantScopeAndDoesNotCapExpirySummary()
    {
        await using var context = CreateTenantContext();
        var now = DateTime.UtcNow;
        context.TblEmployees.AddRange(
            Employee(1, EmployeeType.Manager),
            Employee(2, EmployeeType.Sale));
        context.TblContracts.Add(Contract(
            100,
            1,
            ContractStatus.PendingApproval,
            1,
            "VND",
            now));
        for (var index = 0; index < 9; index++)
        {
            context.TblContracts.Add(Contract(
                200 + index,
                2,
                ContractStatus.Signed,
                1,
                "VND",
                now,
                now.AddDays(index + 1)));
        }
        await context.SaveChangesAsync();

        var result = await new DashboardService(context).GetAsync(
            1,
            new DashboardFilterRequest
            {
                From = now.AddDays(-1),
                To = now.AddDays(1),
                ExpiryDays = 30
            });

        Assert.Equal("Tenant", result.Scope);
        Assert.Equal(10, result.Summary.Single(item => item.Key == "total").Count);
        Assert.Equal(9, result.Summary.Single(item => item.Key == "expiring").Count);
        Assert.Equal(8, result.ExpiringContracts.Count);
    }

    [Fact]
    public async Task AdminDashboard_UsesCentralDataAndRedactsProvisioningError()
    {
        await using var context = CreateCentralContext();
        var now = DateTime.UtcNow;
        context.Tenants.AddRange(
            Tenant(1, "active", TenantStatus.Active, now),
            Tenant(2, "failed", TenantStatus.Failed, now, "Server=secret;Password=secret"));
        context.SystemAdmins.Add(new SystemAdmin
        {
            SystemAdminId = 7,
            Username = "root",
            PasswordHash = "not-returned",
            FullName = "Central Admin",
            IsActive = true,
            CreatedAt = now
        });
        context.SecurityAudits.Add(new CentralSecurityAudit
        {
            CentralSecurityAuditId = 10,
            ActorSystemAdminId = 7,
            TenantId = 2,
            TenantCode = "failed",
            Action = "SystemAdminLogin",
            Result = AuthorizationAuditResultTypes.Failed,
            FailureCode = "InvalidCredentials",
            OccurredAt = now,
            CorrelationId = "correlation-10"
        });
        await context.SaveChangesAsync();

        var result = await new AdminDashboardService(context).GetAsync(
            new AdminDashboardFilterRequest
            {
                From = now.AddDays(-1),
                To = now.AddDays(1)
            });
        var json = JsonSerializer.Serialize(result);

        Assert.Equal(2, result.Summary.Single(item => item.Key == "total").Count);
        Assert.Equal("Central Admin", Assert.Single(result.RecentAudits).ActorDisplayName);
        Assert.Equal("TenantProvisioningFailed", Assert.Single(result.ProvisioningFailures).FailureCode);
        Assert.DoesNotContain("Server=secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SystemHealth_ReturnsOperationalMetadataWithoutSecretsOrPaths()
    {
        await using var context = CreateCentralContext();
        context.Tenants.Add(Tenant(
            1,
            "failed",
            TenantStatus.Failed,
            DateTime.UtcNow,
            "Server=secret;Password=secret"));
        await context.SaveChangesAsync();
        var service = new SystemHealthService(
            context,
            new HealthyStorageProbe(),
            Options.Create(new CustomerOtpOptions
            {
                Provider = "Fake",
                ProviderApiKey = "otp-secret",
                ProviderEndpoint = "https://secret.example"
            }),
            Options.Create(new TemplatePdfRenderingOptions
            {
                ExecutablePath = "C:\\secret\\soffice.exe"
            }));

        var result = await service.GetDetailedAsync();
        var json = JsonSerializer.Serialize(result);

        Assert.Equal(1, result.FailedTenantCount);
        Assert.Null(result.OtpDelivery.BacklogCount);
        Assert.Equal("NotCollected", result.OtpDelivery.BacklogCollection);
        Assert.DoesNotContain("otp-secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret.example", json, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Server=secret", json, StringComparison.Ordinal);
    }

    private static DbDtctechContext CreateTenantContext() => new(
        new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static CentralDbContext CreateCentralContext() => new(
        new DbContextOptionsBuilder<CentralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TblEmployee Employee(int id, EmployeeType type) => new()
    {
        EmployeeId = id,
        EmployeeAccount = $"employee-{id}",
        EmployeeFullName = $"Employee {id}",
        Status = 1,
        EmployeeType = (byte)type,
        RowVersion = [1]
    };

    private static TblContract Contract(
        int id,
        int employeeId,
        ContractStatus status,
        decimal amount,
        string currency,
        DateTime createdAt,
        DateTime? expiresAt = null) => new()
    {
        ContractId = id,
        CustomerId = id,
        EmployeeId = employeeId,
        CreatedEmployeeId = employeeId,
        ContractName = $"Contract {id}",
        ContractCode = $"HD-{id}",
        Status = (byte)status,
        TotalAmount = amount,
        CurrencyCode = currency,
        CreatedDate = createdAt,
        ExpireDate = expiresAt,
        RowVersion = [1]
    };

    private static TblContractAudit Audit(
        int id,
        int contractId,
        int employeeId,
        DateTime occurredAt,
        string actorName) => new()
    {
        ContractAuditId = id,
        TenantId = 1,
        ContractId = contractId,
        SubjectType = "Contract",
        SubjectId = contractId,
        ActorType = "Employee",
        ActorEmployeeId = employeeId,
        ActorDisplayNameSnapshot = actorName,
        ContractCodeSnapshot = $"HD-{contractId}",
        ActionType = "DraftUpdated",
        Result = "Succeeded",
        OccurredAt = occurredAt,
        CorrelationId = $"correlation-{id}"
    };

    private static Tenant Tenant(
        int id,
        string code,
        TenantStatus status,
        DateTime createdAt,
        string? error = null) => new()
    {
        TenantId = id,
        TenantCode = code,
        TenantName = $"Tenant {code}",
        TenantDatabaseId = id,
        Status = status,
        CreatedAt = createdAt,
        UpdatedAt = createdAt,
        ProvisioningError = error
    };

    private sealed class HealthyStorageProbe : IPrivateFileStorageHealthProbe
    {
        public Task<PrivateFileStorageHealthResult> CheckHealthAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrivateFileStorageHealthResult(
                true,
                true,
                1024 * 1024,
                1024));
    }
}
