namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Phân loại dữ liệu mà DOCX renderer sẽ xử lý ở các slice tiếp theo.
/// </summary>
public enum TemplatePlaceholderDataKind
{
    Scalar = 1,
    DynamicBlock = 2
}

/// <summary>
/// Số lần một placeholder được phép xuất hiện trong DOCX.
/// </summary>
public enum TemplatePlaceholderMultiplicity
{
    ExactlyOne = 1,
    ZeroOrOne = 2
}

/// <summary>
/// Một mục trong catalog cố định của SoftwareSupply.
/// Catalog là policy tĩnh, không đọc hoặc ghi tbl_ContractTemplateField.
/// </summary>
public sealed record SoftwareSupplyPlaceholderDefinition(
    string Key,
    string Label,
    bool IsRequired,
    TemplatePlaceholderDataKind DataKind,
    TemplatePlaceholderMultiplicity Multiplicity,
    string DataSource)
{
    public int? Id { get; init; }
    public bool IsSystem { get; init; } = true;
    public bool IsActive { get; init; } = true;
    public string? SourceFieldKey { get; init; }
    public string? ModuleKey { get; init; }
    public PlaceholderValueType? ValueType { get; init; }
    public string? DefaultValue { get; init; }
    public string? FormatString { get; init; }
    public string? RowVersion { get; init; }
}

/// <summary>
/// Catalog V2 cho template hợp đồng cung cấp phần mềm.
///
/// Tenant chỉ cấu hình template/version/term. Tenant không được thay đổi
/// key, DataSource, requiredness hoặc multiplicity của catalog này.
/// </summary>
public static class SoftwareSupplyPlaceholderCatalog
{
    public const string Version = "V2";

    private static readonly IReadOnlyList<SoftwareSupplyPlaceholderDefinition>
        Items =
        [
            new("CONTRACT_CODE", "Mã hợp đồng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.ContractCode"),
            new("CONTRACT_NAME", "Tên hợp đồng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.ContractName"),
            new("CONTRACT_DATE", "Ngày hợp đồng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.CreatedDate"),
            new("EFFECTIVE_DATE", "Ngày hiệu lực", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.EffectiveDate"),
            new("EXPIRE_DATE", "Ngày hết hạn", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.ExpireDate"),
            new("CONTRACT_CURRENCY", "Loại tiền", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.CurrencyCode"),
            new("CUSTOMER_CODE", "Mã khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerCode"),
            new("CUSTOMER_NAME", "Tên khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerFullName"),
            new("CUSTOMER_COMPANY", "Công ty khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerCompany"),
            new("CUSTOMER_TAX_CODE", "Mã số thuế khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerTaxCode"),
            new("CUSTOMER_ADDRESS", "Địa chỉ khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerAddress"),
            new("CUSTOMER_EMAIL", "Email khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerEmail"),
            new("CUSTOMER_PHONE", "Điện thoại khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerPhone"),
            new("PROVIDER_LEGAL_NAME", "Tên pháp nhân bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.LegalEntityName"),
            new("PROVIDER_TAX_CODE", "Mã số thuế bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.TaxCode"),
            new("PROVIDER_ADDRESS", "Địa chỉ bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.Address"),
            new("PROVIDER_REPRESENTATIVE_NAME", "Người đại diện pháp luật bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.RepresentativeName"),
            new("PROVIDER_REPRESENTATIVE_TITLE", "Chức danh người đại diện bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.RepresentativeTitle"),
            new("CONTRACT_TERMS", "Các điều khoản hợp đồng", false, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.Terms"),
            new("CONTRACT_ITEM_TABLE", "Bảng sản phẩm/dịch vụ", false, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.Items"),
            new("SIGNATURE_PROVIDER", "Chữ ký bên cung cấp", false, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.ProviderSignature"),
            new("SIGNATURE_CUSTOMER", "Chữ ký khách hàng", false, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.CustomerSignature"),
            new("CONTRACT_NAME_EN", "Tên hợp đồng tiếng Anh", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.ContractNameEn"),
            new("CUSTOMER_FAX", "Fax khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerFaxNumber"),
            new("CUSTOMER_BANK_ACCOUNT_NUMBER", "Số tài khoản ngân hàng khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerBankAccountNumber"),
            new("CUSTOMER_BANK_NAME", "Tên ngân hàng khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerBankName"),
            new("PROVIDER_PHONE", "Điện thoại bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.PhoneNumber"),
            new("PROVIDER_FAX", "Fax bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.FaxNumber"),
            new("PROVIDER_BANK_ACCOUNT_NUMBER", "Số tài khoản ngân hàng bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.BankAccountNumber"),
            new("PROVIDER_BANK_NAME", "Tên ngân hàng bên cung cấp", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "TenantLegalProfile.BankName"),
            new("CUSTOMER_WEBSITE", "Website khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerWebsite"),
            new("CUSTOMER_CITY", "Tỉnh/thành khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerCity"),
            new("CUSTOMER_COUNTRY", "Quốc gia khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerCountry"),
            new("CONTRACT_TOTAL_AMOUNT", "Tổng giá trị hợp đồng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.TotalAmount"),
            new("CONTRACT_TOTAL_AMOUNT_IN_WORDS", "Tổng giá trị bằng chữ", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Manual.ContractTotalAmountInWords"),
            new("PAYMENT_SCHEDULE_TABLE", "Bảng lịch thanh toán", false, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.PaymentSchedules")
        ];

    public static IReadOnlyList<SoftwareSupplyPlaceholderDefinition> All => Items;

    public static IReadOnlyList<SoftwareSupplyPlaceholderDefinition> GetAll() => Items;

    public static SoftwareSupplyPlaceholderDefinition? Find(string key)
    {
        var normalized = key?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : Items.FirstOrDefault(item =>
                string.Equals(item.Key, normalized, StringComparison.Ordinal));
    }
}
