using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.MultiTenancy.Services;

/// <summary>
/// Tạo database và áp dụng toàn bộ application migrations.
/// </summary>
public sealed class EfCoreTenantDatabaseInitializer
    : ITenantDatabaseInitializer
{
    public async Task InitializeAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        /*
         * Tenant mới chưa nằm trong một HTTP request đã resolve.
         *
         * Vì vậy không lấy DbDtctechContext động từ DI.
         * Ta chủ động tạo context với connection string mới.
         */
        var optionsBuilder =
            new DbContextOptionsBuilder<DbDtctechContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();

                /*
                 * Migrations nằm cùng assembly Infrastructure.
                 */
                sqlOptions.MigrationsAssembly(
                    typeof(DbDtctechContext)
                        .Assembly
                        .GetName()
                        .Name);
            });

        await using var dbContext =
            new DbDtctechContext(optionsBuilder.Options);

        /*
         * MigrateAsync:
         *
         * - Tạo database nếu chưa tồn tại.
         * - Tạo __EFMigrationsHistory.
         * - Chạy các migration chưa áp dụng.
         */
        await dbContext.Database.MigrateAsync(
            cancellationToken);
    }
}