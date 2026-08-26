using System.Text.Json;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.API.Domains.Models.Contract;

/// <summary>
/// Schema pháp lý bất biến dùng chung cho renderer và artifact của SoftwareSupply.
/// Không chứa RowVersion hoặc dữ liệu master có thể thay đổi sau submit.
/// </summary>
public sealed record SoftwareSupplyContractSnapshot(
    int SchemaVersion,
    TenantLegalSnapshot Tenant,
    CustomerLegalSnapshot Customer,
    ContractLegalSnapshot Contract,
    ContractVersionLegalSnapshot Version,
    IReadOnlyList<ContractItemLegalSnapshot> Items,
    IReadOnlyList<ContractTermLegalSnapshot> Terms);

public sealed record TenantLegalSnapshot(
    string LegalEntityName,
    string TaxCode,
    string Address,
    string RepresentativeName,
    string RepresentativeTitle,
    string? PhoneNumber,
    string? FaxNumber,
    string? BankAccountNumber,
    string? BankName);

public sealed record CustomerLegalSnapshot(
    int CustomerId,
    string LegalName,
    string? TaxCode,
    string Address,
    string RepresentativeName,
    string RepresentativeTitle,
    string? PhoneNumber,
    string? FaxNumber,
    string? BankAccountNumber,
    string? BankName);

public sealed record ContractLegalSnapshot(
    int ContractId,
    string ContractCode,
    string ContractName,
    string? ContractNameEn,
    byte ContractType,
    int? TemplateVersionId,
    DateTime CreatedDate,
    DateTime? SignDate,
    DateTime? EffectiveDate,
    DateTime? ExpireDate,
    string CurrencyCode,
    byte LanguageMode,
    decimal Subtotal,
    decimal TotalDiscount,
    decimal TotalVat,
    decimal TotalAmount);

public sealed record ContractVersionLegalSnapshot(
    int VersionId,
    int VersionNo,
    int? SourceVersionId,
    int? TemplateVersionId,
    string CurrencyCode,
    decimal Subtotal,
    decimal TotalDiscount,
    decimal TotalVat,
    decimal TotalAmount);

public sealed record ContractItemLegalSnapshot(
    int ContractItemId,
    byte ItemType,
    string? ItemCode,
    string ItemName,
    string? ItemNameEn,
    string? ItemDescription,
    string? ItemDescriptionEn,
    string? UnitName,
    string? UnitNameEn,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    byte DiscountMode,
    decimal DiscountPercent,
    decimal FixedDiscountAmount,
    decimal DiscountAmount,
    bool IsTaxable,
    decimal VatPercent,
    decimal VatAmount,
    decimal LineTotal,
    int DisplayOrder);

public sealed record ContractTermLegalSnapshot(
    int TermId,
    string TermCode,
    string TermTitle,
    string? TermTitleEn,
    string? TermContent,
    string? TermContentEn,
    bool IsNegotiable,
    int DisplayOrder);

public static class SoftwareSupplyContractSnapshotFactory
{
    public const int CurrentSchemaVersion = 4;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static SoftwareSupplyContractSnapshot Create(
        TblTenantLegalProfile tenant,
        TblCustomer customer,
        TblContract contract,
        TblContractVersion version,
        IEnumerable<TblContractItem> items,
        IEnumerable<TblContractTerm> terms)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(terms);

        var customerLegalName = FirstRequired(
            customer.CustomerCompany,
            customer.CustomerFullName,
            "Tên pháp lý khách hàng");
        var representativeName = Required(
            customer.CustomerRepresentativeName,
            "Người đại diện khách hàng");

        return new SoftwareSupplyContractSnapshot(
            CurrentSchemaVersion,
            new TenantLegalSnapshot(
                Required(tenant.LegalEntityName, "Tên pháp nhân tenant"),
                Required(tenant.TaxCode, "Mã số thuế tenant"),
                Required(tenant.Address, "Địa chỉ tenant"),
                Required(tenant.RepresentativeName, "Người đại diện tenant"),
                Required(tenant.RepresentativeTitle, "Chức danh đại diện tenant"),
                Optional(tenant.PhoneNumber),
                Optional(tenant.FaxNumber),
                Optional(tenant.BankAccountNumber),
                Optional(tenant.BankName)),
            new CustomerLegalSnapshot(
                customer.CustomerId,
                customerLegalName,
                customer.CustomerTaxCode?.Trim(),
                Required(customer.CustomerAddress, "Địa chỉ khách hàng"),
                representativeName,
                Required(
                    customer.CustomerRepresentativeTitle,
                    "Chức danh đại diện khách hàng"),
                FirstOptional(customer.CustomerPhone, customer.CustomerMobile),
                Optional(customer.CustomerFaxNumber),
                Optional(customer.CustomerBankAccountNumber),
                Optional(customer.CustomerBankName)),
            new ContractLegalSnapshot(
                contract.ContractId,
                Required(contract.ContractCode, "Mã hợp đồng"),
                Required(contract.ContractName, "Tên hợp đồng"),
                contract.ContractNameEn?.Trim(),
                contract.ContractType,
                contract.TemplateVersionId,
                contract.CreatedDate,
                contract.SignDate,
                contract.EffectiveDate,
                contract.ExpireDate,
                Required(contract.CurrencyCode, "Tiền tệ"),
                contract.LanguageMode,
                contract.Subtotal,
                contract.TotalDiscount,
                contract.TotalVat,
                contract.TotalAmount),
            new ContractVersionLegalSnapshot(
                version.VersionId,
                version.VersionNo,
                version.SourceVersionId,
                version.TemplateVersionId,
                Required(version.CurrencyCode, "Tiền tệ version"),
                version.Subtotal,
                version.TotalDiscount,
                version.TotalVat,
                version.TotalAmount),
            items.OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ContractItemId)
                .Select(x => new ContractItemLegalSnapshot(
                    x.ContractItemId,
                    x.ItemType,
                    x.ItemCode,
                    x.ItemName,
                    x.ItemNameEn,
                    x.ItemDescription,
                    x.ItemDescriptionEn,
                    x.UnitName,
                    x.UnitNameEn,
                    x.Quantity,
                    x.UnitPrice,
                    x.LineSubtotal,
                    x.DiscountMode,
                    x.DiscountPercent,
                    x.FixedDiscountAmount,
                    x.DiscountAmount,
                    x.IsTaxable,
                    x.VatPercent,
                    x.VatAmount,
                    x.LineTotal,
                    x.DisplayOrder))
                .ToArray(),
            terms.OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.TermId)
                .Select(x => new ContractTermLegalSnapshot(
                    x.TermId,
                    x.TermCode,
                    x.TermTitle,
                    x.TermTitleEn,
                    x.TermContent,
                    x.TermContentEn,
                    x.IsNegotiable,
                    x.DisplayOrder))
                .ToArray());
    }

    public static string Serialize(SoftwareSupplyContractSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    private static string FirstRequired(
        string? preferred,
        string? fallback,
        string fieldName)
    {
        return !string.IsNullOrWhiteSpace(preferred)
            ? preferred.Trim()
            : Required(fallback, fieldName);
    }

    private static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} chưa được cấu hình.");
        }

        return value.Trim();
    }

    private static string? FirstOptional(string? preferred, string? fallback)
    {
        return Optional(preferred) ?? Optional(fallback);
    }

    private static string? Optional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
