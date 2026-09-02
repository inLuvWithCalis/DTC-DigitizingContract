using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public sealed class CentralSeedData : ICentralSeedData
{
    private readonly CentralDbContext _centralDbContext;
    private readonly IPasswordHasher<SystemAdmin> _passwordHasher;
    private readonly SystemAdminBootstrapOptions _options;
    private readonly ILogger<CentralSeedData> _logger;

    public CentralSeedData(
        CentralDbContext centralDbContext,
        IPasswordHasher<SystemAdmin> passwordHasher,
        IOptions<SystemAdminBootstrapOptions> options,
        ILogger<CentralSeedData> logger)
    {
        _centralDbContext = centralDbContext;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var normalizedUsername = _options.Username.Trim();
        bool systemAdminExists =
            await _centralDbContext.SystemAdmins
                .AnyAsync(
                    admin => admin.Username == normalizedUsername,
                    cancellationToken);

        if (systemAdminExists)
        {
            _logger.LogInformation(
                "System Admin bootstrap skipped because account {Username} already exists.",
                normalizedUsername);
            return;
        }

        var systemAdmin = new SystemAdmin
        {
            Username = normalizedUsername,
            FullName = _options.FullName.Trim(),
            Email = _options.Email.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MustChangePassword = true,
            SessionVersion = 1
        };

        systemAdmin.PasswordHash =
            _passwordHasher.HashPassword(
                systemAdmin,
                _options.Password);

        _centralDbContext.SystemAdmins.Add(systemAdmin);

        await _centralDbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogWarning(
            "System Admin bootstrap created account {Username}. Disable SystemAdminBootstrap immediately.",
            normalizedUsername);
    }
}
