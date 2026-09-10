using System.Globalization;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>Explicit business-field allowlist. No entity reflection or client-supplied paths.</summary>
public sealed class ContractPlaceholderSourceRegistry : IContractPlaceholderSourceRegistry
{
    private readonly Dictionary<string, ContractPlaceholderSourceBinding> _sources;

    public ContractPlaceholderSourceRegistry() : this([
        new ContractPlaceholderSourceProvider(),
        new ContractVersionPlaceholderSourceProvider(),
        new CustomerPlaceholderSourceProvider(),
        new TenantLegalProfilePlaceholderSourceProvider(),
        new ContractOwnerPlaceholderSourceProvider(),
        new DepartmentPlaceholderSourceProvider(),
    ]) { }

    public ContractPlaceholderSourceRegistry(IEnumerable<IContractPlaceholderSourceProvider> providers)
    {
        // Duplicate keys fail instead of selecting an arbitrary provider.
        _sources = providers.SelectMany(p => p.GetBindings()).ToDictionary(x => x.Field.SourceFieldKey, StringComparer.Ordinal);
    }

    public IReadOnlyList<ContractPlaceholderSourceField> GetAllFields() => _sources.Values.Select(x => x.Field).ToList();
    public ContractPlaceholderSourceField GetRequired(string key) =>
        _sources.TryGetValue(key, out var source) ? source.Field
            : throw new PlaceholderOperationException("PlaceholderSourceUnsupported", "Trường dữ liệu không được hỗ trợ.");

    public object? Resolve(string key, ContractPlaceholderResolveContext context)
    {
        GetRequired(key);
        return _sources[key].Read(context);
    }

    public string Format(string key, object? value, string? format, string? defaultValue)
    {
        var field = GetRequired(key);
        if (format is not null && !field.AllowedFormats.Contains(format, StringComparer.Ordinal))
            throw new PlaceholderOperationException("PlaceholderFormatInvalid", "Định dạng không phù hợp với trường dữ liệu.");
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            return defaultValue ?? string.Empty;
        return FormatValue(value, format ?? field.AllowedFormats.FirstOrDefault());
    }

    internal static string FormatValue(object value, string? format) => value switch
    {
        bool flag => format == "Yes/No" ? (flag ? "Yes" : "No") : (flag ? "Có" : "Không"),
        IFormattable formatted => formatted.ToString(format, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
