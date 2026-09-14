using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Domains.Services.ContractTemplate;

// Providers read the preloaded render context; no per-field database queries.
public abstract class ContractPlaceholderSourceProviderBase : IContractPlaceholderSourceProvider
{
    private readonly List<ContractPlaceholderSourceBinding> _bindings = [];
    public IReadOnlyList<ContractPlaceholderSourceBinding> GetBindings() => _bindings;
    protected void Add(string key, string module, string moduleLabel, string label, PlaceholderValueType type,
        Func<ContractPlaceholderResolveContext, object?> read)
    {
        string[] formats = type switch
        {
            PlaceholderValueType.Number => ["N0", "N2", "0.##"],
            PlaceholderValueType.Date => ["dd/MM/yyyy", "yyyy-MM-dd"],
            PlaceholderValueType.DateTime => ["dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm"],
            PlaceholderValueType.Boolean => ["Có/Không", "Yes/No"],
            _ => []
        };
        object sample = type switch
        {
            PlaceholderValueType.Number => 1250000.5m,
            PlaceholderValueType.Date or PlaceholderValueType.DateTime => new DateTime(2026, 9, 10, 14, 30, 0),
            PlaceholderValueType.Boolean => true,
            _ => $"{label} mẫu"
        };
        var samples = formats.ToDictionary(f => f, f => ContractPlaceholderSourceRegistry.FormatValue(sample, f), StringComparer.Ordinal);
        _bindings.Add(new(new(key, module, moduleLabel, label, type, true, formats,
            ContractPlaceholderSourceRegistry.FormatValue(sample, formats.FirstOrDefault()), samples), read));
    }

}

public sealed class ContractPlaceholderSourceProvider : ContractPlaceholderSourceProviderBase
{
    public ContractPlaceholderSourceProvider()
    {
        Add("contract.code", "contract", "Hợp đồng", "Mã hợp đồng", PlaceholderValueType.Text, c => c.Contract.ContractCode);
        Add("contract.name", "contract", "Hợp đồng", "Tên hợp đồng", PlaceholderValueType.Text, c => c.Contract.ContractName);
        Add("contract.name-en", "contract", "Hợp đồng", "Tên tiếng Anh", PlaceholderValueType.Text, c => c.Contract.ContractNameEn);
        Add("contract.created-date", "contract", "Hợp đồng", "Ngày hợp đồng", PlaceholderValueType.Date, c => c.Contract.CreatedDate);
        Add("contract.effective-date", "contract", "Hợp đồng", "Ngày hiệu lực", PlaceholderValueType.Date, c => c.Contract.EffectiveDate);
        Add("contract.expire-date", "contract", "Hợp đồng", "Ngày hết hạn", PlaceholderValueType.Date, c => c.Contract.ExpireDate);
        Add("contract.sign-date", "contract", "Hợp đồng", "Ngày ký", PlaceholderValueType.Date, c => c.Contract.SignDate);
    }
}

public sealed class ContractVersionPlaceholderSourceProvider : ContractPlaceholderSourceProviderBase
{
    public ContractVersionPlaceholderSourceProvider()
    {
        Add("version.currency", "version", "Phiên bản hợp đồng", "Tiền tệ", PlaceholderValueType.Text, c => c.Version.CurrencyCode);
        Add("version.total-amount", "version", "Phiên bản hợp đồng", "Tổng giá trị", PlaceholderValueType.Number, c => c.Version.TotalAmount);
        Add("version.subtotal", "version", "Phiên bản hợp đồng", "Thành tiền", PlaceholderValueType.Number, c => c.Version.Subtotal);
        Add("version.total-discount", "version", "Phiên bản hợp đồng", "Tổng giảm giá", PlaceholderValueType.Number, c => c.Version.TotalDiscount);
        Add("version.total-vat", "version", "Phiên bản hợp đồng", "Tổng thuế", PlaceholderValueType.Number, c => c.Version.TotalVat);
        Add("version.number", "version", "Phiên bản hợp đồng", "Số phiên bản", PlaceholderValueType.Number, c => c.Version.VersionNo);
    }
}

public sealed class CustomerPlaceholderSourceProvider : ContractPlaceholderSourceProviderBase
{
    public CustomerPlaceholderSourceProvider()
    {
        Add("customer.code", "customer", "Khách hàng", "Mã khách hàng", PlaceholderValueType.Text, c => c.Customer.CustomerCode);
        Add("customer.name", "customer", "Khách hàng", "Họ tên", PlaceholderValueType.Text, c => c.Customer.CustomerFullName);
        Add("customer.company", "customer", "Khách hàng", "Công ty", PlaceholderValueType.Text, c => c.Customer.CustomerCompany);
        Add("customer.email", "customer", "Khách hàng", "Email", PlaceholderValueType.Text, c => c.Customer.CustomerEmail);
        Add("customer.phone", "customer", "Khách hàng", "Điện thoại", PlaceholderValueType.Text, c => c.Customer.CustomerPhone);
        Add("customer.mobile", "customer", "Khách hàng", "Di động", PlaceholderValueType.Text, c => c.Customer.CustomerMobile);
        Add("customer.address", "customer", "Khách hàng", "Địa chỉ", PlaceholderValueType.Text, c => c.Customer.CustomerAddress);
        Add("customer.tax-code", "customer", "Khách hàng", "Mã số thuế", PlaceholderValueType.Text, c => c.Customer.CustomerTaxCode);
        Add("customer.representative-name", "customer", "Khách hàng", "Đại diện pháp luật", PlaceholderValueType.Text, c => c.Customer.CustomerRepresentativeName);
        Add("customer.representative-title", "customer", "Khách hàng", "Chức danh đại diện", PlaceholderValueType.Text, c => c.Customer.CustomerRepresentativeTitle);
        Add("customer.contact-name", "customer", "Khách hàng", "Người liên hệ", PlaceholderValueType.Text, c => c.Customer.CustomerContactPersonName);
        Add("customer.contact-phone", "customer", "Khách hàng", "Điện thoại liên hệ", PlaceholderValueType.Text, c => c.Customer.CustomerContactPersonPhone);
        Add("customer.contact-title", "customer", "Khách hàng", "Chức danh liên hệ", PlaceholderValueType.Text, c => c.Customer.CustomerContactPersonTitle);
        Add("customer.fax", "customer", "Khách hàng", "Fax", PlaceholderValueType.Text, c => c.Customer.CustomerFaxNumber);
        Add("customer.bank-account", "customer", "Khách hàng", "Số tài khoản", PlaceholderValueType.Text, c => c.Customer.CustomerBankAccountNumber);
        Add("customer.bank-name", "customer", "Khách hàng", "Ngân hàng", PlaceholderValueType.Text, c => c.Customer.CustomerBankName);
        Add("customer.website", "customer", "Khách hàng", "Website", PlaceholderValueType.Text, c => c.Customer.CustomerWebsite);
        Add("customer.city", "customer", "Khách hàng", "Tỉnh/thành", PlaceholderValueType.Text, c => c.Customer.CustomerCity);
        Add("customer.country", "customer", "Khách hàng", "Quốc gia", PlaceholderValueType.Text, c => c.Customer.CustomerCountry);
        Add("customer.zip-code", "customer", "Khách hàng", "Mã bưu chính", PlaceholderValueType.Text, c => c.Customer.CustomerZipCode);
    }
}

public sealed class TenantLegalProfilePlaceholderSourceProvider : ContractPlaceholderSourceProviderBase
{
    public TenantLegalProfilePlaceholderSourceProvider()
    {
        Add("provider.legal-name", "provider", "Pháp nhân doanh nghiệp", "Tên pháp nhân", PlaceholderValueType.Text, c => c.Tenant?.LegalEntityName);
        Add("provider.tax-code", "provider", "Pháp nhân doanh nghiệp", "Mã số thuế", PlaceholderValueType.Text, c => c.Tenant?.TaxCode);
        Add("provider.address", "provider", "Pháp nhân doanh nghiệp", "Địa chỉ", PlaceholderValueType.Text, c => c.Tenant?.Address);
        Add("provider.representative-name", "provider", "Pháp nhân doanh nghiệp", "Đại diện pháp luật", PlaceholderValueType.Text, c => c.Tenant?.RepresentativeName);
        Add("provider.representative-title", "provider", "Pháp nhân doanh nghiệp", "Chức danh đại diện", PlaceholderValueType.Text, c => c.Tenant?.RepresentativeTitle);
        Add("provider.phone", "provider", "Pháp nhân doanh nghiệp", "Điện thoại", PlaceholderValueType.Text, c => c.Tenant?.PhoneNumber);
        Add("provider.fax", "provider", "Pháp nhân doanh nghiệp", "Fax", PlaceholderValueType.Text, c => c.Tenant?.FaxNumber);
        Add("provider.bank-account", "provider", "Pháp nhân doanh nghiệp", "Số tài khoản", PlaceholderValueType.Text, c => c.Tenant?.BankAccountNumber);
        Add("provider.bank-name", "provider", "Pháp nhân doanh nghiệp", "Ngân hàng", PlaceholderValueType.Text, c => c.Tenant?.BankName);
    }
}

public sealed class ContractOwnerPlaceholderSourceProvider : ContractPlaceholderSourceProviderBase
{
    public ContractOwnerPlaceholderSourceProvider()
    {
        Add("contract-owner.full-name", "contract-owner", "Nhân viên phụ trách", "Họ tên", PlaceholderValueType.Text, c => c.Owner?.EmployeeFullName);
        Add("contract-owner.code", "contract-owner", "Nhân viên phụ trách", "Mã nhân viên", PlaceholderValueType.Text, c => c.Owner?.EmployeeCode);
        Add("contract-owner.email", "contract-owner", "Nhân viên phụ trách", "Email", PlaceholderValueType.Text, c => c.Owner?.EmployeeEmail);
        Add("contract-owner.phone", "contract-owner", "Nhân viên phụ trách", "Điện thoại", PlaceholderValueType.Text, c => c.Owner?.EmployeePhone);
        Add("contract-owner.mobile", "contract-owner", "Nhân viên phụ trách", "Di động", PlaceholderValueType.Text, c => c.Owner?.EmployeeMobile);
    }
}

public sealed class DepartmentPlaceholderSourceProvider : ContractPlaceholderSourceProviderBase
{
    public DepartmentPlaceholderSourceProvider()
    {
        Add("department.code", "department", "Phòng ban phụ trách", "Mã phòng ban", PlaceholderValueType.Text, c => c.Department?.DepartmentCode);
        Add("department.name", "department", "Phòng ban phụ trách", "Tên phòng ban", PlaceholderValueType.Text, c => c.Department?.DepartmentName);
    }
}
