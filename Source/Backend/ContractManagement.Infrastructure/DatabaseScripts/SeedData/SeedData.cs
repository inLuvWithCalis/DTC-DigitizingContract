using ContractManagement.Infrastructure.MultiTenancy.Contracts;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public sealed class TenantSeedData : ITenantSeedData
{
    private readonly IPasswordHasher<TblEmployee> _passwordHasher;

    public TenantSeedData(IPasswordHasher<TblEmployee> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync(
        string connectionString,
        int tenantId,
        InitialManagerProvisioningCommand initialManager,
        SecurityOperationContext securityContext,
        CancellationToken cancellationToken = default)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbDtctechContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.MigrationsAssembly(
                typeof(DbDtctechContext).Assembly.GetName().Name);
        });

        await using var dbContext = new DbDtctechContext(optionsBuilder.Options);
        var account = NormalizeRequired(initialManager.EmployeeAccount);

        if (await dbContext.TblEmployees.AnyAsync(
                employee => employee.EmployeeAccount == account,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Tài khoản initial Manager đã tồn tại trong tenant.");
        }

        var manager = new TblEmployee
        {
            EmployeeCode = Normalize(initialManager.EmployeeCode),
            EmployeeAccount = account,
            EmployeeFullName = NormalizeRequired(initialManager.EmployeeFullName),
            EmployeeMobile = Normalize(initialManager.EmployeeMobile),
            EmployeeEmail = Normalize(initialManager.EmployeeEmail),
            EmployeeType = 6,
            Status = 1,
            DateCreated = DateTime.UtcNow,
            MustChangePassword = true,
            SessionVersion = 1
        };
        manager.EmployeePassword = _passwordHasher.HashPassword(
            manager,
            NormalizeRequired(initialManager.EmployeePassword));

        dbContext.TblEmployees.Add(manager);
        dbContext.TblAuthorizationAudits.Add(
            AuthorizationAuditRecordFactory.CreateTenant(
                tenantId,
                null,
                "SystemAdmin",
                AuthorizationAuditActionTypes.EmployeeCreated,
                AuthorizationAuditResultTypes.Success,
                "Employee",
                manager.EmployeeAccount,
                null,
                manager.EmployeeType,
                null,
                manager.Status,
                null,
                DateTime.UtcNow,
                securityContext.IpAddress,
                securityContext.UserAgent,
                securityContext.CorrelationId));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Initial Manager bắt buộc có thông tin đăng nhập hợp lệ.");
        }

        return value.Trim();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
