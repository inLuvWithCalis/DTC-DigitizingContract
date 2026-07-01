using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Options;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Central;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ContractManagement.Infrastructure.DatabaseScripts.SeedData;
using Microsoft.AspNetCore.Identity;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Infrastructure.MultiTenancy.DI;

/// <summary>
/// Đăng ký toàn bộ service của Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection
        AddContractManagementInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(
            configuration.GetSection(
                MultiTenancyOptions.SectionName));

        string centralConnectionString =
            configuration.GetConnectionString(
                "CentralDatabase")
            ?? throw new InvalidOperationException(
                "Không tìm thấy connection string "
                + "'CentralDatabase'.");

        /*
         * CentralDbContext luôn kết nối một database cố định.
         */
        services.AddDbContext<CentralDbContext>(
            options =>
            {
                options.UseSqlServer(
                    centralConnectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure();

                        sqlOptions.MigrationsAssembly(
                            typeof(CentralDbContext)
                                .Assembly
                                .GetName()
                                .Name);
                    });
            });

        /*
         * Mỗi HTTP request có một CurrentTenant riêng.
         */
        services.AddScoped<
            ICurrentTenant,
            CurrentTenant>();

        services.AddScoped<
            ITenantResolver,
            TenantResolver>();

        /*
         * DbDtctechContext dùng connection string động.
         *
         * Khi context được tạo:
         * 1. Lấy tenant của request.
         * 2. Lấy connection string tenant.
         * 3. Kết nối đúng database.
         */
        services.AddDbContext<DbDtctechContext>(
            (serviceProvider, options) =>
            {
                var currentTenant =
                    serviceProvider
                        .GetRequiredService<ICurrentTenant>()
                        .GetRequiredTenant();

                options.UseSqlServer(
                    currentTenant.ConnectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure();

                        sqlOptions.MigrationsAssembly(
                            typeof(DbDtctechContext)
                                .Assembly
                                .GetName()
                                .Name);
                    });
            });

        services.AddScoped<
            ITenantDatabaseInitializer,
            EfCoreTenantDatabaseInitializer>();

        services.AddScoped<
            ITenantProvisioningService,
            TenantProvisioningService>();

        /*
         * Dang ký các service dùng để seed dữ liệu tenant.
        */
        services.AddScoped<
            IPasswordHasher<TblEmployee>,
            PasswordHasher<TblEmployee>>();

        services.AddScoped<
            ITenantSeedData,
            TenantSeedData>();

        return services;
    }
}