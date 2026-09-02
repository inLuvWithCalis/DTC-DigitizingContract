using ContractManagement.Infrastructure.DatabaseScripts.SeedData;
using ContractManagement.Infrastructure.MultiTenancy.Contracts;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Integration;

public sealed class Phase1AccountSecuritySqlServerMigrationTests
{
    private const string ConnectionVariable =
        "PHASE12_SQLSERVER_CONNECTION";

    [Phase1SqlServerFact]
    [Trait("Category", "Phase1SqlServer")]
    public async Task BlankCentralAndTenantDatabases_MigrateAccountSecuritySchema()
    {
        var baseConnection = Environment.GetEnvironmentVariable(
            ConnectionVariable)!;
        var runId = Guid.NewGuid().ToString("N");
        var centralDatabase = $"ContractManagement_Phase1_QA_Central_{runId}";
        var tenantDatabase = $"ContractManagement_Phase1_QA_Tenant_{runId}";
        var centralConnection = WithDatabase(baseConnection, centralDatabase);
        var tenantConnection = WithDatabase(baseConnection, tenantDatabase);

        try
        {
            await AssertCentralMigrationAsync(centralConnection);
            await AssertTenantMigrationAsync(tenantConnection);
        }
        finally
        {
            await DropCentralDatabaseAsync(centralConnection, centralDatabase);
            await DropTenantDatabaseAsync(tenantConnection, tenantDatabase);
        }
    }

    private static async Task AssertCentralMigrationAsync(string connectionString)
    {
        await using var context = CreateCentralContext(connectionString);
        var migrations = context.Database.GetMigrations().ToArray();
        var accountMigrationIndex = Array.FindIndex(
            migrations,
            migration => migration.EndsWith(
                "_AddSystemAdminAccountSecurity",
                StringComparison.Ordinal));
        Assert.True(accountMigrationIndex > 0);
        await context.Database.MigrateAsync(migrations[accountMigrationIndex - 1]);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO [SystemAdmins]
                ([Username], [PasswordHash], [FullName], [Email], [IsActive], [CreatedAt])
            VALUES
                ('phase1-legacy-admin', 'phase-1-legacy-hash', N'Legacy Admin', NULL, 1, SYSUTCDATETIME())
            """);

        await context.Database.MigrateAsync();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());

        var migratedAdmin = await context.SystemAdmins
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Username == "phase1-legacy-admin");
        Assert.NotEmpty(migratedAdmin.RowVersion);
        Assert.False(migratedAdmin.MustChangePassword);
        Assert.Equal(1, migratedAdmin.SessionVersion);
        Assert.Null(migratedAdmin.AvatarStorageKey);
        Assert.Null(migratedAdmin.CoverStorageKey);

        var admin = new SystemAdmin
        {
            Username = "phase1-admin",
            PasswordHash = "phase-1-test-hash",
            FullName = "Phase 1 Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow,
            MustChangePassword = true,
            SessionVersion = 2
        };
        context.SystemAdmins.Add(admin);
        await context.SaveChangesAsync();

        Assert.NotEmpty(admin.RowVersion);
        Assert.True(admin.MustChangePassword);
        Assert.Equal(2, admin.SessionVersion);
    }

    private static async Task AssertTenantMigrationAsync(string connectionString)
    {
        await using var context = CreateTenantContext(connectionString);
        var migrations = context.Database.GetMigrations().ToArray();
        var accountMigrationIndex = Array.FindIndex(
            migrations,
            migration => migration.EndsWith(
                "_AddEmployeeAccountSecurity",
                StringComparison.Ordinal));
        Assert.True(accountMigrationIndex > 0);
        await context.Database.MigrateAsync(migrations[accountMigrationIndex - 1]);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO [tbl_Employee]
                ([EmployeeCode], [EmployeeAccount], [EmployeePassword], [EmployeeFullName], [Status], [EmployeeType], [DateCreated])
            VALUES
                ('PH1-LEGACY', 'phase1-legacy', 'phase-1-legacy-hash', N'Legacy Employee', 1, 1, GETUTCDATE())
            """);

        await context.Database.MigrateAsync();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());

        var migratedEmployee = await context.TblEmployees
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EmployeeCode == "PH1-LEGACY");
        Assert.NotEmpty(migratedEmployee.RowVersion);
        Assert.False(migratedEmployee.MustChangePassword);
        Assert.Equal(1, migratedEmployee.SessionVersion);
        Assert.Null(migratedEmployee.AvatarStorageKey);
        Assert.Null(migratedEmployee.CoverStorageKey);

        var employee = new TblEmployee
        {
            EmployeeCode = "PH1-EMPLOYEE",
            EmployeeAccount = "phase1-employee",
            EmployeePassword = "phase-1-test-hash",
            EmployeeFullName = "Phase 1 Employee",
            Status = 1,
            EmployeeType = 1,
            DateCreated = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow,
            MustChangePassword = true,
            SessionVersion = 2
        };
        context.TblEmployees.Add(employee);
        await context.SaveChangesAsync();

        Assert.NotEmpty(employee.RowVersion);
        Assert.True(employee.MustChangePassword);
        Assert.Equal(2, employee.SessionVersion);

        var tenantSeedData = new TenantSeedData(
            new PasswordHasher<TblEmployee>());
        await tenantSeedData.InitializeAsync(
            connectionString,
            101,
            new InitialManagerProvisioningCommand(
                "PH1-MANAGER",
                "phase1-manager",
                "TemporaryPassword123!",
                "Phase 1 Manager",
                null,
                null),
            new SecurityOperationContext(
                1,
                null,
                null,
                "phase1-migration-test"));

        context.ChangeTracker.Clear();
        var initialManager = await context.TblEmployees
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EmployeeCode == "PH1-MANAGER");
        Assert.True(initialManager.MustChangePassword);
        Assert.Equal(1, initialManager.SessionVersion);
    }

    private static CentralDbContext CreateCentralContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CentralDbContext>()
            .UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure();
                    sqlOptions.MigrationsAssembly(
                        typeof(CentralDbContext).Assembly.GetName().Name);
                })
            .Options;
        return new CentralDbContext(options);
    }

    private static DbDtctechContext CreateTenantContext(string connectionString)
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
        EnsureQaDatabaseName(databaseName);
        return new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = databaseName
        }.ConnectionString;
    }

    private static async Task DropCentralDatabaseAsync(
        string connectionString,
        string databaseName)
    {
        EnsureQaDatabaseName(databaseName);
        await using var context = CreateCentralContext(connectionString);
        await context.Database.EnsureDeletedAsync();
    }

    private static async Task DropTenantDatabaseAsync(
        string connectionString,
        string databaseName)
    {
        EnsureQaDatabaseName(databaseName);
        await using var context = CreateTenantContext(connectionString);
        await context.Database.EnsureDeletedAsync();
    }

    private static void EnsureQaDatabaseName(string databaseName)
    {
        if (!databaseName.StartsWith(
                "ContractManagement_Phase1_QA_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Phase 1 test refuses to use a non-QA database name.");
        }
    }

    private sealed class Phase1SqlServerFactAttribute : FactAttribute
    {
        public Phase1SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable(ConnectionVariable)))
            {
                Skip = $"Set {ConnectionVariable} to run the real SQL Server migration gate.";
            }
        }
    }
}
