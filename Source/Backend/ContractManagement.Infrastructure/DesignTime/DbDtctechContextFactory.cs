using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ContractManagement.Infrastructure.DesignTime;

/// <summary>
/// Giúp EF Core Tools tạo DbDtctechContext
/// khi chạy các lệnh migration.
///
/// Factory này chỉ dùng ở design time.
/// Runtime vẫn lấy connection string theo tenant hiện tại.
/// </summary>
public sealed class DbDtctechContextFactory
    : IDesignTimeDbContextFactory<DbDtctechContext>
{
    public DbDtctechContext CreateDbContext(string[] args)
    {
        /*
         * Đọc appsettings.json từ project Web API.
         */
        var configuration =
            DesignTimeConfiguration.Build();

        /*
         * Lấy connection string mẫu.
         *
         * Connection string này chỉ cung cấp:
         * - SQL Server
         * - tài khoản đăng nhập
         * - các tùy chọn kết nối
         *
         * Runtime không sử dụng database master
         * làm database nghiệp vụ.
         */
        string connectionString =
            configuration.GetConnectionString(
                "TenantDatabaseTemplate")
            ?? throw new InvalidOperationException(
                "Không tìm thấy connection string "
                + "'TenantDatabaseTemplate'.");

        var optionsBuilder =
            new DbContextOptionsBuilder<DbDtctechContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                /*
                 * Migration của DbDtctechContext
                 * nằm trong assembly Infrastructure.
                 */
                sqlOptions.MigrationsAssembly(
                    typeof(DbDtctechContext)
                        .Assembly
                        .GetName()
                        .Name);
            });

        return new DbDtctechContext(
            optionsBuilder.Options);
    }
}