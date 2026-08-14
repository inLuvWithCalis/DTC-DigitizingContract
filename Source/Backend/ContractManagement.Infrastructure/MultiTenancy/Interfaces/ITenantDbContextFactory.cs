using ContractManagement.Infrastructure.Persistence.Application;

namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

public interface ITenantDbContextFactory
{
    DbDtctechContext Create(string connectionString);
}
