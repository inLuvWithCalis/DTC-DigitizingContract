using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public interface IContractPlaceholderCatalog
{
    Task<IReadOnlyList<SoftwareSupplyPlaceholderDefinition>> GetAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
}

public interface IContractPlaceholderSourceRegistry
{
    IReadOnlyList<ContractPlaceholderSourceField> GetAllFields();
    ContractPlaceholderSourceField GetRequired(string key);
    object? Resolve(string key, ContractPlaceholderResolveContext context);
    string Format(string key, object? value, string? format, string? defaultValue);
}

public sealed record ContractPlaceholderSourceBinding(ContractPlaceholderSourceField Field,
    Func<ContractPlaceholderResolveContext, object?> Read);

public interface IContractPlaceholderSourceProvider
{
    IReadOnlyList<ContractPlaceholderSourceBinding> GetBindings();
}
