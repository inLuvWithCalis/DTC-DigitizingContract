using ContractManagement.API.Common.Enums;

namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Dữ liệu giả cố định cho DOCX preview SoftwareSupply.
/// Dataset này tuyệt đối không đọc Contract, Customer, Tenant hoặc nhân sự thật.
/// </summary>
public static class SoftwareSupplyPreviewDatasetV1
{
    public const string Version = "V2";

    public const string LegalDisclaimer =
        "DỮ LIỆU MẪU — KHÔNG CÓ GIÁ TRỊ PHÁP LÝ";

    private static readonly IReadOnlySet<string> Keys = new HashSet<string>(
        StringComparer.Ordinal)
    {
        "CONTRACT_CODE",
        "CONTRACT_NAME",
        "CONTRACT_DATE",
        "EFFECTIVE_DATE",
        "EXPIRE_DATE",
        "CONTRACT_CURRENCY",
        "CUSTOMER_CODE",
        "CUSTOMER_NAME",
        "CUSTOMER_REPRESENTATIVE_TITLE",
        "CUSTOMER_COMPANY",
        "CUSTOMER_TAX_CODE",
        "CUSTOMER_ADDRESS",
        "CUSTOMER_EMAIL",
        "CUSTOMER_PHONE",
        "PROVIDER_LEGAL_NAME",
        "PROVIDER_TAX_CODE",
        "PROVIDER_ADDRESS",
        "PROVIDER_REPRESENTATIVE_NAME",
        "PROVIDER_REPRESENTATIVE_TITLE",
        "CONTRACT_TERMS",
        "CONTRACT_ITEM_TABLE",
        "SIGNATURE_PROVIDER",
        "SIGNATURE_CUSTOMER",
        "CONTRACT_NAME_EN",
        "CUSTOMER_FAX",
        "CUSTOMER_BANK_ACCOUNT_NUMBER",
        "CUSTOMER_BANK_NAME",
        "PROVIDER_PHONE",
        "PROVIDER_FAX",
        "PROVIDER_BANK_ACCOUNT_NUMBER",
        "PROVIDER_BANK_NAME",
        "CUSTOMER_WEBSITE",
        "CUSTOMER_CITY",
        "CUSTOMER_COUNTRY",
        "CONTRACT_TOTAL_AMOUNT",
        "CONTRACT_TOTAL_AMOUNT_IN_WORDS",
        "PAYMENT_SCHEDULE_TABLE"
    };

    public static IReadOnlySet<string> CoveredPlaceholderKeys => Keys;

    public static IReadOnlyDictionary<string, string> GetScalarValues(
        ContractLanguageMode languageMode)
    {
        var contractName = languageMode == ContractLanguageMode.Bilingual
            ? "HỢP ĐỒNG CUNG CẤP PHẦN MỀM / SOFTWARE SUPPLY AGREEMENT — " +
              LegalDisclaimer
            : "HỢP ĐỒNG CUNG CẤP PHẦN MỀM — " + LegalDisclaimer;

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CONTRACT_CODE"] = "PREVIEW-SS-V1-2026-0001",
            ["CONTRACT_NAME"] = contractName,
            ["CONTRACT_DATE"] = "Ngày 15 tháng 08 năm 2026",
            ["EFFECTIVE_DATE"] = "Ngày 01 tháng 09 năm 2026",
            ["EXPIRE_DATE"] = "Ngày 31 tháng 08 năm 2027",
            ["CONTRACT_CURRENCY"] = "VND",
            ["CUSTOMER_CODE"] = "CUS-DEMO-2026",
            ["CUSTOMER_NAME"] = "Trần Thị Mẫu",
            ["CUSTOMER_REPRESENTATIVE_TITLE"] = "Tổng giám đốc",
            ["CUSTOMER_COMPANY"] =
                "CÔNG TY CỔ PHẦN GIẢ LẬP GIẢI PHÁP DOANH NGHIỆP MINH AN",
            ["CUSTOMER_TAX_CODE"] = "0312345678",
            ["CUSTOMER_ADDRESS"] =
                "Tầng 18, Tòa nhà Minh An Demo, 123 Đường Mẫu, Phường Bến Thành, Quận 1, TP. Hồ Chí Minh, Việt Nam",
            ["CUSTOMER_EMAIL"] = "legal.demo.customer@example.invalid",
            ["CUSTOMER_PHONE"] = "+84 28 7300 0001",
            ["PROVIDER_LEGAL_NAME"] = "CÔNG TY CỔ PHẦN DTC MẪU",
            ["PROVIDER_TAX_CODE"] = "0107654321",
            ["PROVIDER_ADDRESS"] =
                "Tầng 10, Tòa nhà DTC Demo, Hà Nội, Việt Nam",
            ["PROVIDER_REPRESENTATIVE_NAME"] = "Nguyễn Văn Mẫu",
            ["PROVIDER_REPRESENTATIVE_TITLE"] = "Giám đốc",
            ["CONTRACT_NAME_EN"] =
                "SOFTWARE SUPPLY AGREEMENT — SAMPLE DATA — NO LEGAL VALUE",
            ["CUSTOMER_FAX"] = "+84 28 7300 0002",
            ["CUSTOMER_BANK_ACCOUNT_NUMBER"] = "012345678901",
            ["CUSTOMER_BANK_NAME"] = "Ngân hàng TMCP Khách hàng Mẫu",
            ["PROVIDER_PHONE"] = "+84 24 7300 0001",
            ["PROVIDER_FAX"] = "+84 24 7300 0002",
            ["PROVIDER_BANK_ACCOUNT_NUMBER"] = "098765432109",
            ["PROVIDER_BANK_NAME"] = "Ngân hàng TMCP Nhà cung cấp Mẫu",
            ["CUSTOMER_WEBSITE"] = "https://customer-demo.example.invalid",
            ["CUSTOMER_CITY"] = "Hồ Chí Minh",
            ["CUSTOMER_COUNTRY"] = "Việt Nam",
            ["CONTRACT_TOTAL_AMOUNT"] = "72.187.500",
            ["CONTRACT_TOTAL_AMOUNT_IN_WORDS"] =
                "Bảy mươi hai triệu một trăm tám mươi bảy nghìn năm trăm đồng chẵn"
        };
    }

    public static IReadOnlyList<SoftwareSupplyPreviewLineItem> Items { get; } =
    [
        new(
            1,
            "Sản phẩm",
            "Nền tảng quản trị hợp đồng doanh nghiệp phiên bản mẫu với bộ quy trình phê duyệt đa cấp, nhật ký kiểm toán và thư viện biểu mẫu mở rộng",
            3,
            12_500_000m,
            5m,
            10m),
        new(
            2,
            "Dịch vụ",
            "Dịch vụ triển khai, cấu hình và hỗ trợ vận hành từ xa cho hệ thống mẫu trong 12 tháng, bao gồm đào tạo người dùng chủ chốt",
            12,
            2_500_000m,
            0m,
            10m)
    ];

    public static IReadOnlyList<SoftwareSupplyPreviewPayment> Payments { get; } =
    [
        new(1, "Đợt 1 — sau khi ký hợp đồng mẫu", 50m, 36_093_750m,
            "Trong vòng 05 ngày làm việc kể từ ngày ký."),
        new(2, "Đợt 2 — sau nghiệm thu mẫu", 50m, 36_093_750m,
            "Trong vòng 05 ngày làm việc kể từ ngày nghiệm thu.")
    ];

    public static IReadOnlyList<SoftwareSupplyPreviewTerm> Terms { get; } =
    [
        new(
            1,
            "Phạm vi cung cấp",
            "Scope of supply",
            "Bên Cung Cấp cung cấp phần mềm, tài liệu hướng dẫn, cấu hình mẫu và dịch vụ hỗ trợ theo bảng hạng mục. Mọi tên, số liệu và thời hạn trong bản preview này chỉ được tạo để kiểm tra bố cục tài liệu; chúng không tạo ra cam kết, nghĩa vụ hay quyền lợi đối với bất kỳ tổ chức hoặc cá nhân nào.",
            "The Provider supplies the software, user documentation, sample configuration and support services listed in the item table. Every name, amount and date in this preview exists solely to verify document layout and creates no commitment, obligation or right for any person or organization."),
        new(
            2,
            "Triển khai và nghiệm thu",
            "Implementation and acceptance",
            "Hai Bên giả định phối hợp chuẩn bị môi trường, cấu hình quy trình và thực hiện nghiệm thu theo kế hoạch mẫu. Tiêu chí nghiệm thu, người phê duyệt và biên bản được thể hiện ở đây chỉ là nội dung minh họa, không được sử dụng để triển khai thực tế.",
            "The Parties are assumed to prepare the environment, configure workflows and perform acceptance under a sample plan. The acceptance criteria, approvers and records shown here are illustrative only and must not be used for actual delivery."),
        new(
            3,
            "Bảo mật và dữ liệu",
            "Confidentiality and data",
            "Không có dữ liệu khách hàng, hợp đồng, tenant, nhân sự hoặc chữ ký thật nào được đưa vào preview. Bản preview chỉ sử dụng dữ liệu giả cố định và không phải là hồ sơ lưu trữ, chứng cứ giao dịch hoặc tài liệu có giá trị pháp lý.",
            "No real customer, contract, tenant, employee or signature data is included in this preview. It uses fixed fictitious data only and is not a record, transaction evidence or legally binding document."),
        new(
            4,
            "Hiệu lực bản mẫu",
            "Sample document status",
            "Bản DOCX preview được sinh riêng từ template Draft đã được validation. Template gốc không bị sửa đổi; mọi thay đổi đối với DOCX gốc, catalog, dataset hoặc chế độ ngôn ngữ sẽ làm preview cũ không còn hiện hành.",
            "This DOCX preview is generated separately from a validated Draft template. The source template is not modified; changes to the source DOCX, catalog, dataset or language mode make an earlier preview non-current.")
    ];

    public static SoftwareSupplyPreviewSignature ProviderSignature { get; } = new(
        "ĐẠI DIỆN BÊN CUNG CẤP",
        "Nguyễn Văn Mẫu — Giám đốc dự án (dữ liệu mẫu)");

    public static SoftwareSupplyPreviewSignature CustomerSignature { get; } = new(
        "ĐẠI DIỆN BÊN KHÁCH HÀNG",
        "Trần Thị Mẫu — Người đại diện (dữ liệu mẫu)");
}

public sealed record SoftwareSupplyPreviewLineItem(
    int No,
    string Type,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal VatPercent)
{
    public decimal GrossAmount => Quantity * UnitPrice;

    public decimal DiscountAmount => GrossAmount * DiscountPercent / 100m;

    public decimal NetAmount => GrossAmount - DiscountAmount;

    public decimal VatAmount => NetAmount * VatPercent / 100m;

    public decimal TotalAmount => NetAmount + VatAmount;
}

public sealed record SoftwareSupplyPreviewPayment(
    int No,
    string Description,
    decimal Percent,
    decimal Amount,
    string DueCondition);

public sealed record SoftwareSupplyPreviewTerm(
    int No,
    string TitleVi,
    string TitleEn,
    string ContentVi,
    string ContentEn);

public sealed record SoftwareSupplyPreviewSignature(
    string PartyTitle,
    string SignerName);
