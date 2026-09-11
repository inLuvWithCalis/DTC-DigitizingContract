using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractManagement.Domains.Services.ContractTemplate;

public sealed class ContractPlaceholderCatalog(
    DbDtctechContext db, IContractPlaceholderSourceRegistry registry,
    IOptions<CustomContractPlaceholderOptions>? options = null,
    ICurrentTenant? tenant = null) : IContractPlaceholderCatalog
{
    public bool CustomEnabled => options?.Value.Enabled == true &&
        (options.Value.TenantCodes.Length == 0 ||
         tenant?.Value is { } current && options.Value.TenantCodes.Contains(current.TenantCode, StringComparer.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<SoftwareSupplyPlaceholderDefinition>> GetAsync(
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = SystemDefinitions.ToList();
        if (!CustomEnabled) return result;
        var custom = await db.TblContractPlaceholderDefinitions.AsNoTracking()
            .Where(x => includeInactive || x.IsActive).OrderBy(x => x.PlaceholderKey).ToListAsync(cancellationToken);
        result.AddRange(custom.Select(ToDefinition));
        return result;
    }

    public SoftwareSupplyPlaceholderDefinition ToDefinition(TblContractPlaceholderDefinition item)
    {
        var field = registry.GetRequired(item.SourceFieldKey);
        return new(item.PlaceholderKey, item.FieldLabel, false, TemplatePlaceholderDataKind.Scalar,
            TemplatePlaceholderMultiplicity.ZeroOrOne, $"{field.ModuleLabel} / {field.FieldLabel}")
        {
            Id = item.PlaceholderDefinitionId, IsSystem = false, IsActive = item.IsActive,
            SourceFieldKey = item.SourceFieldKey, ModuleKey = field.ModuleKey, ValueType = field.ValueType,
            DefaultValue = item.DefaultValue, FormatString = item.FormatString,
            RowVersion = Convert.ToBase64String(item.RowVersion)
        };
    }

    public static IReadOnlyList<SoftwareSupplyPlaceholderDefinition> SystemDefinitions =>
        SoftwareSupplyPlaceholderCatalog.All.Select(x => x with { SourceFieldKey = "system." + x.Key }).ToArray();

    public static SoftwareSupplyPlaceholderDefinition FromSnapshot(TblContractTemplateField f) =>
        new(f.PlaceholderKey, f.FieldLabel, f.IsRequired,
            (TemplatePlaceholderDataKind)f.DataKind,
            (TemplatePlaceholderMultiplicity)f.Multiplicity, f.DataSource)
        {
            IsSystem = f.IsSystem,
            SourceFieldKey = f.SourceFieldKey,
            DefaultValue = f.DefaultValue,
            FormatString = f.FormatString,
            RowVersion = f.DefinitionRowVersion is null
                ? null
                : Convert.ToBase64String(f.DefinitionRowVersion)
        };

    public static string Fingerprint(IEnumerable<SoftwareSupplyPlaceholderDefinition> definitions) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            definitions.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new
            {
                x.Key, x.SourceFieldKey, x.DefaultValue, x.FormatString, x.DataKind,
                x.Multiplicity, x.IsRequired, x.IsSystem, x.IsActive, x.RowVersion
            }))))).ToLowerInvariant();
}
