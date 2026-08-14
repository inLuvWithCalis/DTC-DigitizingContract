using ContractManagement.Infrastructure.DatabaseScripts.SeedData;
using ContractManagement.Infrastructure.MultiTenancy.Contracts;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Options;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Central;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContractManagement.Tests.Domains.Security;

public sealed class TenantProvisioningServiceTests
{
    [Fact]
    public async Task Provisioning_ActivatesTenantOnlyAfterInitialManagerSeedAndWritesCentralAudit()
    {
        await using var centralDbContext = CreateCentralDbContext();
        var initializer = new RecordingInitializer();
        var seedData = new RecordingSeedData();
        var service = CreateService(centralDbContext, initializer, seedData);
        var command = NewCommand("tenant-a");

        var result = await service.CreateDedicatedAsync(command);

        Assert.Equal(TenantStatus.Active, result.Status);
        Assert.True(initializer.WasCalled);
        Assert.NotNull(seedData.InitialManager);
        Assert.Equal("first-manager", seedData.InitialManager!.EmployeeAccount);
        var audit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal("TenantProvisioned", audit.Action);
        Assert.Equal("Success", audit.Result);
        Assert.Equal((byte)6, audit.NewEmployeeType);
        Assert.Equal((byte)1, audit.NewStatus);
    }

    [Fact]
    public async Task ProvisioningFailure_LeavesTenantFailedAndWritesFailedCentralAudit()
    {
        await using var centralDbContext = CreateCentralDbContext();
        var service = CreateService(
            centralDbContext,
            new RecordingInitializer(),
            new RecordingSeedData(new InvalidOperationException("seed failure")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateDedicatedAsync(NewCommand("tenant-b")));

        var tenant = await centralDbContext.Tenants.SingleAsync();
        Assert.Equal(TenantStatus.Failed, tenant.Status);
        Assert.Contains("seed failure", tenant.ProvisioningError);
        var audit = await centralDbContext.SecurityAudits.SingleAsync();
        Assert.Equal("Failed", audit.Result);
        Assert.Equal("ProvisioningFailed", audit.FailureCode);
    }

    private static TenantProvisioningService CreateService(
        CentralDbContext centralDbContext,
        ITenantDatabaseInitializer initializer,
        ITenantSeedData seedData)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TenantDatabaseTemplate"] =
                    "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=template;Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();

        return new TenantProvisioningService(
            centralDbContext,
            initializer,
            Options.Create(new MultiTenancyOptions
            {
                DatabasePrefix = "ContractManagement_Tenant_",
                TemplateConnectionName = "TenantDatabaseTemplate"
            }),
            configuration,
            NullLogger<TenantProvisioningService>.Instance,
            seedData);
    }

    private static TenantProvisioningCommand NewCommand(string tenantCode) => new(
        tenantCode,
        "Tenant Name",
        new InitialManagerProvisioningCommand(
            "MGR-001",
            "first-manager",
            "Password123!",
            "First Manager",
            null,
            null),
        new SecurityOperationContext(
            1,
            "127.0.0.1",
            "unit-test",
            "provisioning-test"));

    private static CentralDbContext CreateCentralDbContext()
    {
        return new CentralDbContext(
            new DbContextOptionsBuilder<CentralDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private sealed class RecordingInitializer : ITenantDatabaseInitializer
    {
        public bool WasCalled { get; private set; }

        public Task InitializeAsync(
            string connectionString,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSeedData : ITenantSeedData
    {
        private readonly Exception? _exception;

        public RecordingSeedData(Exception? exception = null)
        {
            _exception = exception;
        }

        public InitialManagerProvisioningCommand? InitialManager { get; private set; }

        public Task InitializeAsync(
            string connectionString,
            int tenantId,
            InitialManagerProvisioningCommand initialManager,
            SecurityOperationContext securityContext,
            CancellationToken cancellationToken = default)
        {
            InitialManager = initialManager;
            return _exception is null
                ? Task.CompletedTask
                : Task.FromException(_exception);
        }
    }
}
