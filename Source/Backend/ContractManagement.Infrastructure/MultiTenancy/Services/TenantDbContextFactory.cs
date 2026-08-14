using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.MultiTenancy.Services;

public sealed class TenantDbContextFactory : ITenantDbContextFactory
{
    public DbDtctechContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
                sqlOptions.MigrationsAssembly(
                    typeof(DbDtctechContext).Assembly.GetName().Name);
            })
            .Options;

        return new DbDtctechContext(options);
    }
}
