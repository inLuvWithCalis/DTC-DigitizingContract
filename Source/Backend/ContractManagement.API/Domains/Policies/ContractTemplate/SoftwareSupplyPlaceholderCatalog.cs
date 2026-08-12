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
    string DataSource);

/// <summary>
/// Catalog V1 cho template hợp đồng cung cấp phần mềm.
///
/// Tenant chỉ cấu hình template/version/term. Tenant không được thay đổi
/// key, DataSource, requiredness hoặc multiplicity của catalog này.
/// </summary>
public static class SoftwareSupplyPlaceholderCatalog
{
    public const string Version = "V1";

    private static readonly IReadOnlyList<SoftwareSupplyPlaceholderDefinition>
        Items =
        [
            new("CONTRACT_CODE", "Mã hợp đồng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.ContractCode"),
            new("CONTRACT_NAME", "Tên hợp đồng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.ContractName"),
            new("CONTRACT_DATE", "Ngày hợp đồng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.CreatedDate"),
            new("EFFECTIVE_DATE", "Ngày hiệu lực", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.EffectiveDate"),
            new("EXPIRE_DATE", "Ngày hết hạn", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.ExpireDate"),
            new("CONTRACT_CURRENCY", "Loại tiền", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.CurrencyCode"),
            new("CUSTOMER_CODE", "Mã khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerCode"),
            new("CUSTOMER_NAME", "Tên khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerFullName"),
            new("CUSTOMER_COMPANY", "Công ty khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerCompany"),
            new("CUSTOMER_TAX_CODE", "Mã số thuế khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerTaxCode"),
            new("CUSTOMER_ADDRESS", "Địa chỉ khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerAddress"),
            new("CUSTOMER_EMAIL", "Email khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerEmail"),
            new("CUSTOMER_PHONE", "Điện thoại khách hàng", true, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ExactlyOne, "Customer.CustomerMobile"),
            new("CONTRACT_TERMS", "Các điều khoản hợp đồng", true, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.Terms"),
            new("CONTRACT_ITEM_TABLE", "Bảng sản phẩm/dịch vụ", true, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.Items"),
            new("SIGNATURE_PROVIDER", "Chữ ký bên cung cấp", true, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.ProviderSignature"),
            new("SIGNATURE_CUSTOMER", "Chữ ký khách hàng", true, TemplatePlaceholderDataKind.DynamicBlock, TemplatePlaceholderMultiplicity.ExactlyOne, "Contract.CustomerSignature"),
            new("CONTRACT_NAME_EN", "Tên hợp đồng tiếng Anh", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Contract.ContractNameEn"),
            new("CUSTOMER_FAX", "Fax khách hàng", false, TemplatePlaceholderDataKind.Scalar, TemplatePlaceholderMultiplicity.ZeroOrOne, "Customer.CustomerFaxNumber"),
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
