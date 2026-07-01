namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public interface ICentralSeedData
{
    Task InitializeAsync(
        CancellationToken cancellationToken = default);
}