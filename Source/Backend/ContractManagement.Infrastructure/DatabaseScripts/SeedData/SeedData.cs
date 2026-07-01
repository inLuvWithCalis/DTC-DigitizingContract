using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public sealed class TenantSeedData : ITenantSeedData
{
    private readonly IPasswordHasher<TblEmployee> _passwordHasher;

    public TenantSeedData(
        IPasswordHasher<TblEmployee> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<DbDtctechContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();

                sqlOptions.MigrationsAssembly(
                    typeof(DbDtctechContext)
                        .Assembly
                        .GetName()
                        .Name);
            });

        await using var dbContext =
            new DbDtctechContext(optionsBuilder.Options);

        bool adminExists =
            await dbContext.TblEmployees
                .AnyAsync(
                    employee =>
                        employee.EmployeeAccount == "admin",
                    cancellationToken);

        if (adminExists)
        {
            return;
        }

        var adminEmployee = new TblEmployee
        {
            EmployeeAccount = "admin",
            EmployeeFullName = "Administrator",
            EmployeeEmail = "admin@example.com"
        };

        adminEmployee.EmployeePassword =
            _passwordHasher.HashPassword(
                adminEmployee,
                "123456");

        dbContext.TblEmployees.Add(adminEmployee);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}