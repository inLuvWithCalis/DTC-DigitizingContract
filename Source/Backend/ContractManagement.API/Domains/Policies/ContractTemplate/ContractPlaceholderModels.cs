using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Policies.ContractTemplate;

public enum PlaceholderValueType { Text = 1, Number = 2, Date = 3, DateTime = 4, Boolean = 5 }

public sealed record ContractPlaceholderSourceField(
    string SourceFieldKey, string ModuleKey, string ModuleLabel, string FieldLabel,
    PlaceholderValueType ValueType, bool IsNullable, IReadOnlyList<string> AllowedFormats,
    string SampleValue, IReadOnlyDictionary<string, string> FormattedSamples);

public sealed record ContractPlaceholderResolveContext(
    TblContract Contract, TblContractVersion Version, TblCustomer Customer,
    TblTenantLegalProfile? Tenant, TblEmployee? Owner, TblDepartment? Department);

public sealed class CustomContractPlaceholderOptions
{
    public const string SectionName = "CustomContractPlaceholders";
    public bool Enabled { get; set; }
    public string[] TenantCodes { get; set; } = [];
}

public sealed class PlaceholderOperationException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}

public sealed record PlaceholderUsage(int TemplateId, int TemplateVersionId, string TemplateCode, int VersionNo, byte Status);
