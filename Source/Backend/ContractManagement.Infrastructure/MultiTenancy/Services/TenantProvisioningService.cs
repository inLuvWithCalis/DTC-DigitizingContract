using ContractManagement.Infrastructure.DatabaseScripts.SeedData;
using ContractManagement.Infrastructure.MultiTenancy.Contracts;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Options;
using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractManagement.Infrastructure.MultiTenancy.Services;

/// <summary>
/// Tạo tenant và database riêng cho tenant.
/// </summary>
public sealed class TenantProvisioningService
    : ITenantProvisioningService
{
    private readonly CentralDbContext _centralDbContext;
    private readonly ITenantDatabaseInitializer _databaseInitializer;
    private readonly MultiTenancyOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly ITenantSeedData _tenantSeedData;

    public TenantProvisioningService(
        CentralDbContext centralDbContext,
        ITenantDatabaseInitializer databaseInitializer,
        IOptions<MultiTenancyOptions> options,
        IConfiguration configuration,
        ILogger<TenantProvisioningService> logger,
        ITenantSeedData tenantSeedData)
    {
        _centralDbContext = centralDbContext;
        _databaseInitializer = databaseInitializer;
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;
        _tenantSeedData = tenantSeedData;
    }

    public async Task<IReadOnlyList<TenantProvisioningResult>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _centralDbContext.Tenants
            .AsNoTracking()
            .Include(tenant => tenant.TenantDatabase)
            .OrderBy(tenant => tenant.TenantCode)
            .Select(tenant => new TenantProvisioningResult(
                tenant.TenantId,
                tenant.TenantCode,
                tenant.TenantName,
                tenant.TenantDatabase.DatabaseName,
                tenant.TenantDatabase.Mode,
                tenant.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantProvisioningResult>
        CreateDedicatedAsync(
            TenantProvisioningCommand command,
            CancellationToken cancellationToken = default)
    {
        string tenantCode =
            command.TenantCode
                .Trim()
                .ToLowerInvariant();

        /*
         * Kiểm tra tenant đã tồn tại chưa.
         */
        bool tenantExists =
            await _centralDbContext.Tenants
                .AnyAsync(
                    x => x.TenantCode == tenantCode,
                    cancellationToken);

        if (tenantExists)
        {
            throw new InvalidOperationException(
                $"Tenant '{tenantCode}' đã tồn tại.");
        }

        /*
         * TenantCode được validate chỉ chứa:
         * chữ thường, số và dấu gạch ngang.
         *
         * Khi đặt tên database, đổi dấu gạch ngang thành gạch dưới.
         */
        string safeSuffix =
            tenantCode.Replace("-", "_");

        string databaseName =
            $"{_options.DatabasePrefix}{safeSuffix}";

        string connectionString =
            BuildConnectionString(databaseName);

        var tenantDatabase = new TenantDatabase
        {
            DatabaseKey = $"dedicated-{tenantCode}",
            DatabaseName = databaseName,
            ConnectionString = connectionString,
            Mode = TenantDatabaseMode.Dedicated,
            CreatedAt = DateTime.UtcNow
        };

        var tenant = new Tenant
        {
            TenantCode = tenantCode,
            TenantName = command.TenantName.Trim(),
            Status = TenantStatus.Provisioning,
            TenantDatabase = tenantDatabase,
            CreatedAt = DateTime.UtcNow
        };

        /*
         * Lưu trạng thái Provisioning trước khi tạo database.
         *
         * Nếu quá trình tạo database lỗi,
         * hệ thống vẫn có bản ghi để kiểm tra và retry.
         */
        _centralDbContext.Tenants.Add(tenant);

        await _centralDbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            await _databaseInitializer.InitializeAsync(
                connectionString,
                cancellationToken);
            
            await _tenantSeedData.InitializeAsync(
                connectionString,
                tenant.TenantId,
                command.InitialManager,
                command.SecurityContext,
                cancellationToken);

            tenant.Status = TenantStatus.Active;
            tenant.ProvisioningError = null;
            tenant.UpdatedAt = DateTime.UtcNow;

            _centralDbContext.SecurityAudits.Add(
                AuthorizationAuditRecordFactory.CreateCentral(
                    command.SecurityContext.SystemAdminId,
                    tenant.TenantId,
                    tenant.TenantCode,
                    AuthorizationAuditActionTypes.TenantProvisioned,
                    AuthorizationAuditResultTypes.Success,
                    "Tenant",
                    tenant.TenantId.ToString(),
                    null,
                    6,
                    null,
                    1,
                    null,
                    DateTime.UtcNow,
                    command.SecurityContext.IpAddress,
                    command.SecurityContext.UserAgent,
                    command.SecurityContext.CorrelationId));

            await _centralDbContext.SaveChangesAsync(
                cancellationToken);

            return new TenantProvisioningResult(
                tenant.TenantId,
                tenant.TenantCode,
                tenant.TenantName,
                tenantDatabase.DatabaseName,
                tenantDatabase.Mode,
                tenant.Status);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Tạo database cho tenant {TenantCode} thất bại.",
                tenantCode);

            tenant.Status = TenantStatus.Failed;
            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.ProvisioningError =
                Truncate(exception.Message, 2000);

            _centralDbContext.SecurityAudits.Add(
                AuthorizationAuditRecordFactory.CreateCentral(
                    command.SecurityContext.SystemAdminId,
                    tenant.TenantId,
                    tenant.TenantCode,
                    AuthorizationAuditActionTypes.TenantProvisioned,
                    AuthorizationAuditResultTypes.Failed,
                    "Tenant",
                    tenant.TenantId.ToString(),
                    null,
                    null,
                    null,
                    null,
                    "ProvisioningFailed",
                    DateTime.UtcNow,
                    command.SecurityContext.IpAddress,
                    command.SecurityContext.UserAgent,
                    command.SecurityContext.CorrelationId));

            await _centralDbContext.SaveChangesAsync(
                cancellationToken);

            throw;
        }
    }

    private string BuildConnectionString(
        string databaseName)
    {
        string templateConnectionString =
            _configuration.GetConnectionString(
                _options.TemplateConnectionName)
            ?? throw new InvalidOperationException(
                $"Không tìm thấy connection string "
                + $"'{_options.TemplateConnectionName}'.");

        /*
         * Không sử dụng string.Replace để thay tên database.
         *
         * SqlConnectionStringBuilder hiểu đúng cấu trúc
         * connection string của SQL Server.
         */
        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                templateConnectionString)
            {
                InitialCatalog = databaseName
            };

        return connectionStringBuilder.ConnectionString;
    }

    private static string Truncate(
        string value,
        int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
