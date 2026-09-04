using System.Diagnostics;
using System.Reflection;
using ContractManagement.API.Domains.CustomerAccess;
using ContractManagement.API.Domains.DTOs.Responses.Admin;
using ContractManagement.API.Domains.Interfaces.Admin;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.Persistence.Central;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractManagement.API.Domains.Services.Admin;

public sealed class SystemHealthService : ISystemHealthService
{
    private readonly CentralDbContext _centralDbContext;
    private readonly IPrivateFileStorageHealthProbe _storageHealthProbe;
    private readonly CustomerOtpOptions _otpOptions;
    private readonly TemplatePdfRenderingOptions _pdfOptions;

    public SystemHealthService(
        CentralDbContext centralDbContext,
        IPrivateFileStorageHealthProbe storageHealthProbe,
        IOptions<CustomerOtpOptions> otpOptions,
        IOptions<TemplatePdfRenderingOptions> pdfOptions)
    {
        _centralDbContext = centralDbContext;
        _storageHealthProbe = storageHealthProbe;
        _otpOptions = otpOptions.Value;
        _pdfOptions = pdfOptions.Value;
    }

    public async Task<SystemHealthResponse> GetDetailedAsync(
        CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTime.UtcNow;
        var central = await CheckCentralDatabaseAsync(cancellationToken);
        var storage = await CheckStorageAsync(cancellationToken);
        var pdfAvailable = LibreOfficeContractTemplatePdfRenderer
            .ResolveExecutablePath(_pdfOptions.ExecutablePath) is not null;
        int? failedTenantCount = null;
        if (central.Status == "Healthy")
        {
            failedTenantCount = await _centralDbContext.Tenants
                .AsNoTracking()
                .CountAsync(tenant => tenant.Status == TenantStatus.Failed,
                    cancellationToken);
        }

        var processStart = Process.GetCurrentProcess().StartTime.ToUniversalTime();
        var status = central.Status == "Unavailable"
            || storage.Status == "Unavailable"
                ? "Unavailable"
                : pdfAvailable ? "Healthy" : "Degraded";

        return new SystemHealthResponse
        {
            Status = status,
            GeneratedAt = generatedAt,
            Api = new ApiRuntimeHealthResponse
            {
                Version = Assembly.GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion
                    ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                    ?? "unknown",
                StartedAt = processStart,
                UptimeSeconds = Math.Max(
                    0,
                    (long)(generatedAt - processStart).TotalSeconds)
            },
            CentralDatabase = central,
            PrivateStorage = storage,
            PdfRenderer = new PdfRendererHealthResponse
            {
                Status = pdfAvailable ? "Healthy" : "Unavailable"
            },
            OtpDelivery = new OtpDeliveryHealthResponse
            {
                ProviderMode = NormalizeProvider(_otpOptions.Provider),
                BacklogCount = null,
                BacklogCollection = "NotCollected"
            },
            SessionStore = new SessionStoreHealthResponse
            {
                Mode = "InMemory"
            },
            FailedTenantCount = failedTenantCount
        };
    }

    public async Task<bool> IsReadyAsync(
        CancellationToken cancellationToken = default)
    {
        var central = await CheckCentralDatabaseAsync(cancellationToken);
        var storage = await CheckStorageAsync(cancellationToken);
        return central.Status == "Healthy" && storage.Status == "Healthy";
    }

    private async Task<DependencyHealthResponse> CheckCentralDatabaseAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _centralDbContext.Database
                .CanConnectAsync(cancellationToken);
            return new DependencyHealthResponse
            {
                Status = canConnect ? "Healthy" : "Unavailable",
                Code = canConnect ? null : "CentralDatabaseUnavailable"
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new DependencyHealthResponse
            {
                Status = "Unavailable",
                Code = "CentralDatabaseUnavailable"
            };
        }
    }

    private async Task<PrivateStorageHealthResponse> CheckStorageAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _storageHealthProbe.CheckHealthAsync(cancellationToken);
            return new PrivateStorageHealthResponse
            {
                Status = result.IsWritable && result.MeetsCapacityThreshold
                    ? "Healthy"
                    : "Unavailable",
                Writable = result.IsWritable,
                MeetsCapacityThreshold = result.MeetsCapacityThreshold,
                AvailableFreeSpaceBytes = result.AvailableFreeSpaceBytes,
                MinimumFreeSpaceBytes = result.MinimumFreeSpaceBytes
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new PrivateStorageHealthResponse
            {
                Status = "Unavailable"
            };
        }
    }

    private static string NormalizeProvider(string? provider)
    {
        var normalized = provider?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Unknown" : normalized;
    }
}
