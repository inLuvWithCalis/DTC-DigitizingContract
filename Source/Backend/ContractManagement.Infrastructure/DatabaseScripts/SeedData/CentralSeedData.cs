using ContractManagement.Infrastructure.Persistence.Central;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public sealed class CentralSeedData : ICentralSeedData
{
    private readonly CentralDbContext _centralDbContext;
    private readonly IPasswordHasher<SystemAdmin> _passwordHasher;

    public CentralSeedData(
        CentralDbContext centralDbContext,
        IPasswordHasher<SystemAdmin> passwordHasher)
    {
        _centralDbContext = centralDbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        bool systemAdminExists =
            await _centralDbContext.SystemAdmins
                .AnyAsync(
                    admin => admin.Username == "sysadmin",
                    cancellationToken);

        if (systemAdminExists)
        {
            return;
        }

        var systemAdmin = new SystemAdmin
        {
            Username = "sysadmin",
            FullName = "System Administrator",
            Email = "sysadmin@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        systemAdmin.PasswordHash =
            _passwordHasher.HashPassword(
                systemAdmin,
                "123456");

        _centralDbContext.SystemAdmins.Add(systemAdmin);

        await _centralDbContext.SaveChangesAsync(
            cancellationToken);
    }
}