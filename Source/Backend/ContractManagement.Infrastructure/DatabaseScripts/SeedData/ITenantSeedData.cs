namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public interface ITenantSeedData
{
    Task InitializeAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}