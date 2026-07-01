using ContractManagement.Infrastructure.Persistence.Central;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ContractManagement.Infrastructure.DesignTime;

/// <summary>
/// Giúp EF Core Tools tạo CentralDbContext
/// khi chạy các lệnh dotnet ef.
/// </summary>
public sealed class CentralDbContextFactory
    : IDesignTimeDbContextFactory<CentralDbContext>
{
    public CentralDbContext CreateDbContext(string[] args)
    {
        /*
         * Đọc cấu hình từ appsettings.json
         * của project Web API ContractManagement.
         */
        var configuration =
            DesignTimeConfiguration.Build();

        /*
         * Lấy connection string của Central Database.
         */
        string connectionString =
            configuration.GetConnectionString("CentralDatabase")
            ?? throw new InvalidOperationException(
                "Không tìm thấy connection string 'CentralDatabase'.");

        var optionsBuilder =
            new DbContextOptionsBuilder<CentralDbContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                /*
                 * Migration của CentralDbContext
                 * nằm trong project Infrastructure.
                 */
                sqlOptions.MigrationsAssembly(
                    typeof(CentralDbContext)
                        .Assembly
                        .GetName()
                        .Name);
            });

        return new CentralDbContext(
            optionsBuilder.Options);
    }
}