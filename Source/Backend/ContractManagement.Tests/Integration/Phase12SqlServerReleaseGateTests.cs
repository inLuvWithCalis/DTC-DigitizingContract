using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Integration;

public sealed class Phase12SqlServerReleaseGateTests
{
    private const string ConnectionVariable =
        "PHASE12_SQLSERVER_CONNECTION";

    [Phase12SqlServerFact]
    [Trait("Category", "Phase12SqlServer")]
    public async Task RealSqlServer_MigratesTwoTenantDatabases_AndEnforcesIsolationRbacAndRowVersion()
    {
        var baseConnection = Environment.GetEnvironmentVariable(
            ConnectionVariable)!;
        var runId = Guid.NewGuid().ToString("N");
        var databaseA = $"ContractManagement_Phase12_QA_A_{runId}";
        var databaseB = $"ContractManagement_Phase12_QA_B_{runId}";
        var connectionA = WithDatabase(baseConnection, databaseA);
        var connectionB = WithDatabase(baseConnection, databaseB);

        try
        {
            await using (var tenantA = CreateContext(connectionA))
            await using (var tenantB = CreateContext(connectionB))
            {
                await tenantA.Database.MigrateAsync();
                await tenantB.Database.MigrateAsync();

                Assert.Empty(await tenantA.Database.GetPendingMigrationsAsync());
                Assert.Empty(await tenantB.Database.GetPendingMigrationsAsync());
            }

            var seeded = await SeedTenantAAsync(connectionA);
            var tenantBEmployeeId = await SeedTenantBEmployeeAsync(connectionB);

            await using (var tenantA = CreateContext(connectionA))
            {
                var authorization = new ContractResourceAuthorizationService(
                    tenantA);

                await authorization.EnsureCanReadAsync(
                    seeded.ContractId,
                    seeded.OwnerId);
                await authorization.EnsureCanWriteAsync(
                    seeded.ContractId,
                    seeded.OwnerId);
                await authorization.EnsureCanReadAsync(
                    seeded.ContractId,
                    seeded.ManagerId);

                var managerWrite = await Assert.ThrowsAsync<RbacOperationException>(
                    () => authorization.EnsureCanWriteAsync(
                        seeded.ContractId,
                        seeded.ManagerId));
                var unrelatedRead = await Assert.ThrowsAsync<RbacOperationException>(
                    () => authorization.EnsureCanReadAsync(
                        seeded.ContractId,
                        seeded.UnrelatedEmployeeId));

                Assert.Equal(
                    AuthorizationErrorCodes.ResourceNotFound,
                    managerWrite.Code);
                Assert.Equal(
                    AuthorizationErrorCodes.ResourceNotFound,
                    unrelatedRead.Code);
            }

            await using (var tenantB = CreateContext(connectionB))
            {
                var crossTenantRead = await Assert.ThrowsAsync<RbacOperationException>(
                    () => new ContractResourceAuthorizationService(tenantB)
                        .EnsureCanReadAsync(
                            seeded.ContractId,
                            tenantBEmployeeId));

                Assert.Equal(
                    AuthorizationErrorCodes.ResourceNotFound,
                    crossTenantRead.Code);
                Assert.Empty(tenantB.TblContracts);
            }

            await AssertSqlServerRowVersionAsync(
                connectionA,
                seeded.ContractId);
        }
        finally
        {
            await DropQaDatabaseAsync(connectionA, databaseA);
            await DropQaDatabaseAsync(connectionB, databaseB);
        }
    }

    private static async Task<SeededTenantA> SeedTenantAAsync(
        string connectionString)
    {
        await using var context = CreateContext(connectionString);
        var owner = Employee("PH12-OWNER", EmployeeType.Sale);
        var manager = Employee("PH12-MANAGER", EmployeeType.Manager);
        var unrelated = Employee("PH12-OTHER", EmployeeType.Marketing);
        context.TblEmployees.AddRange(owner, manager, unrelated);
        await context.SaveChangesAsync();

        var customer = new TblCustomer
        {
            CustomerCode = "PH12-CUSTOMER",
            CustomerFullName = "Phase 12 Customer",
            Status = 1
        };
        context.TblCustomers.Add(customer);
        await context.SaveChangesAsync();

        var contract = new TblContract
        {
            CustomerId = customer.CustomerId,
            EmployeeId = owner.EmployeeId,
            ContractType = (byte)ContractType.SoftwareSupply,
            ContractCode = $"PH12-{Guid.NewGuid():N}",
            ContractName = "Phase 12 SQL Server isolation contract",
            Status = (byte)ContractStatus.Draft,
            TotalAmount = 100m,
            Subtotal = 100m,
            CurrencyCode = "VND",
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            CreatedEmployeeId = owner.EmployeeId,
            CreatedDate = DateTime.UtcNow
        };
        context.TblContracts.Add(contract);
        await context.SaveChangesAsync();

        Assert.NotEmpty(contract.RowVersion);
        return new SeededTenantA(
            contract.ContractId,
            owner.EmployeeId,
            manager.EmployeeId,
            unrelated.EmployeeId);
    }

    private static async Task<int> SeedTenantBEmployeeAsync(
        string connectionString)
    {
        await using var context = CreateContext(connectionString);
        var employee = Employee("PH12-TENANT-B", EmployeeType.Manager);
        context.TblEmployees.Add(employee);
        await context.SaveChangesAsync();
        return employee.EmployeeId;
    }

    private static async Task AssertSqlServerRowVersionAsync(
        string connectionString,
        int contractId)
    {
        await using var firstContext = CreateContext(connectionString);
        await using var staleContext = CreateContext(connectionString);
        var first = await firstContext.TblContracts.SingleAsync(contract =>
            contract.ContractId == contractId);
        var stale = await staleContext.TblContracts.SingleAsync(contract =>
            contract.ContractId == contractId);
        var initialVersion = first.RowVersion.ToArray();

        first.ContractName = "Phase 12 first concurrent update";
        await firstContext.SaveChangesAsync();

        Assert.False(initialVersion.AsSpan().SequenceEqual(first.RowVersion));
        stale.ContractName = "Phase 12 stale concurrent update";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            staleContext.SaveChangesAsync());
    }

    private static TblEmployee Employee(
        string code,
        EmployeeType type) => new()
        {
            EmployeeCode = code,
            EmployeeAccount = code.ToLowerInvariant(),
            EmployeePassword = "phase-12-test-hash",
            EmployeeFullName = code,
            EmployeeType = (byte)type,
            Status = 1,
            DateCreated = DateTime.UtcNow
        };

    private static DbDtctechContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure();
                    sqlOptions.MigrationsAssembly(
                        typeof(DbDtctechContext).Assembly.GetName().Name);
                })
            .Options;
        return new DbDtctechContext(options);
    }

    private static string WithDatabase(
        string baseConnection,
        string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = databaseName
        };
        if (!databaseName.StartsWith(
                "ContractManagement_Phase12_QA_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Phase 12 test refuses to use a non-QA database name.");
        }

        return builder.ConnectionString;
    }

    private static async Task DropQaDatabaseAsync(
        string connectionString,
        string databaseName)
    {
        if (!databaseName.StartsWith(
                "ContractManagement_Phase12_QA_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Phase 12 cleanup refuses to delete a non-QA database.");
        }

        await using var context = CreateContext(connectionString);
        await context.Database.EnsureDeletedAsync();
    }

    private sealed record SeededTenantA(
        int ContractId,
        int OwnerId,
        int ManagerId,
        int UnrelatedEmployeeId);

    private sealed class Phase12SqlServerFactAttribute : FactAttribute
    {
        public Phase12SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable(ConnectionVariable)))
            {
                Skip = $"Set {ConnectionVariable} to run the real SQL Server release gate.";
            }
        }
    }
}
