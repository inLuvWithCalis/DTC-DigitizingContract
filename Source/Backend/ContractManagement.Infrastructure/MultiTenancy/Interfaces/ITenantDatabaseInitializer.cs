namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

/// <summary>
/// Khởi tạo schema cho database tenant mới.
/// </summary>
public interface ITenantDatabaseInitializer
{
    Task InitializeAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}