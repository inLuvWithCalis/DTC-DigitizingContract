using System.Security.Cryptography;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Models.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Renders SoftwareSupply DOCX/PDF from one schema-v4 snapshot. Preview results
/// remain ephemeral; the submit pipeline persists the separate submission result
/// only after both formats have been generated successfully.
/// </summary>
public sealed class ContractDocumentPreviewService :
    IContractDocumentPreviewService,
    IContractSubmissionArtifactRenderer
{
    private const string TemplateVersionObjectType = "ContractTemplateVersion";
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PdfContentType = "application/pdf";

    private readonly DbDtctechContext _dbContext;
    private readonly IContractResourceAuthorizationService _authorization;
    private readonly IFileStorageService _fileStorageService;
    private readonly IContractTemplatePreviewRenderer _documentRenderer;
    private readonly IContractTemplatePdfRenderer _pdfRenderer;

    public ContractDocumentPreviewService(
        DbDtctechContext dbContext,
        IContractResourceAuthorizationService authorization,
        IFileStorageService fileStorageService,
        IContractTemplatePreviewRenderer documentRenderer,
        IContractTemplatePdfRenderer pdfRenderer)
    {
        _dbContext = dbContext;
        _authorization = authorization;
        _fileStorageService = fileStorageService;
        _documentRenderer = documentRenderer;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<ContractDocumentPreviewResult> GenerateDocxAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var rendered = await RenderDocxAsync(
            contractId,
            employeeId,
            requireNegotiating: false,
            cancellationToken);

        return new ContractDocumentPreviewResult(
            rendered.Content,
            $"{rendered.SafeContractCode}-preview.docx",
            DocxContentType);
    }

    public async Task<ContractDocumentPreviewResult> GeneratePdfAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var rendered = await RenderDocxAsync(
            contractId,
            employeeId,
            requireNegotiating: false,
            cancellationToken);
        var pdf = await _pdfRenderer.ConvertPreviewToPdfAsync(
            rendered.Content,
            cancellationToken);

        return new ContractDocumentPreviewResult(
            pdf,
            $"{rendered.SafeContractCode}-preview.pdf",
            PdfContentType);
    }

    public async Task<ContractSubmissionArtifactRenderResult> RenderAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var rendered = await RenderDocxAsync(
            contractId,
            employeeId,
            requireNegotiating: true,
            cancellationToken);
        var pdf = await _pdfRenderer.ConvertPreviewToPdfAsync(
            rendered.Content,
            cancellationToken);

        return new ContractSubmissionArtifactRenderResult(
            SoftwareSupplyContractSnapshotFactory.Serialize(rendered.Snapshot),
            rendered.Snapshot.SchemaVersion,
            rendered.TemplateVersionId,
            rendered.Content,
            $"{rendered.SafeContractCode}-submitted.docx",
            pdf,
            $"{rendered.SafeContractCode}-submitted.pdf");
    }

    private async Task<RenderedContractDocx> RenderDocxAsync(
        int contractId,
        int employeeId,
        bool requireNegotiating,
        CancellationToken cancellationToken)
    {
        // Preview is a write-scope operation: Manager may read another owner's
        // contract but may not render/export its mutable legal content.
        await _authorization.EnsureCanWriteAsync(
            contractId,
            employeeId,
            cancellationToken);

        var contract = await _dbContext.TblContracts
            .AsNoTracking()
            .SingleAsync(item => item.ContractId == contractId, cancellationToken);

        EnsurePreviewPolicy(contract);
        if (requireNegotiating
            && contract.Status != (byte)ContractStatus.Negotiating)
        {
            throw new InvalidOperationException(
                "Chỉ hợp đồng đang đàm phán mới được tạo artifact gửi duyệt.");
        }

        var versionId = contract.CurrentVersionId
            ?? throw new InvalidOperationException(
                "Hợp đồng chưa có phiên bản hiện hành để tạo preview.");
        var version = await _dbContext.TblContractVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.VersionId == versionId
                    && item.ContractId == contractId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Phiên bản hiện hành của hợp đồng không còn khả dụng.");

        var templateVersionId = version.TemplateVersionId
            ?? contract.TemplateVersionId
            ?? throw new InvalidOperationException(
                "Hợp đồng không có template để tạo preview.");

        var template = await (
                from templateVersion in _dbContext.TblContractTemplateVersions
                    .AsNoTracking()
                join templateDefinition in _dbContext.TblContractTemplates
                    .AsNoTracking()
                    on templateVersion.TemplateId equals templateDefinition.TemplateId
                where templateVersion.TemplateVersionId == templateVersionId
                select new { Version = templateVersion, Template = templateDefinition })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Phiên bản template của hợp đồng không còn khả dụng.");

        EnsureTemplatePolicy(template.Version, template.Template, contract);

        var customer = await _dbContext.TblCustomers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CustomerId == contract.CustomerId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Khách hàng của hợp đồng không còn khả dụng.");
        var tenant = await _dbContext.TblTenantLegalProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Hồ sơ pháp lý doanh nghiệp chưa được cấu hình.");
        var items = await _dbContext.TblContractItems
            .AsNoTracking()
            .Where(item => item.ContractId == contractId
                && item.VersionId == versionId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.ContractItemId)
            .ToListAsync(cancellationToken);
        var terms = await _dbContext.TblContractTerms
            .AsNoTracking()
            .Where(term => term.ContractId == contractId
                && term.VersionId == versionId)
            .OrderBy(term => term.DisplayOrder)
            .ThenBy(term => term.TermId)
            .ToListAsync(cancellationToken);
        var payments = await _dbContext.TblPaymentSchedules
            .AsNoTracking()
            .Where(schedule => schedule.ContractId == contractId)
            .OrderBy(schedule => schedule.DueDate)
            .ThenBy(schedule => schedule.ScheduleId)
            .ToListAsync(cancellationToken);

        var snapshot = SoftwareSupplyContractSnapshotFactory.Create(
            tenant,
            customer,
            contract,
            version,
            items,
            terms);
        var renderData = CreateRenderData(snapshot, customer, payments);
        var source = await ReadTemplateSourceAsync(
            template.Version,
            cancellationToken);
        var renderedDocx = _documentRenderer.Render(
            source,
            (ContractLanguageMode)snapshot.Contract.LanguageMode,
            renderData);

        return new RenderedContractDocx(
            renderedDocx,
            SafeFileName(snapshot.Contract.ContractCode, contractId),
            snapshot,
            templateVersionId);
    }

    private static void EnsurePreviewPolicy(TblContract contract)
    {
        if ((ContractStatus)contract.Status is not (
            ContractStatus.Draft or ContractStatus.Negotiating))
        {
            throw new InvalidOperationException(
                "Chỉ hợp đồng nháp hoặc đang đàm phán mới được tạo bản preview động.");
        }

        if (contract.ContractType != (byte)ContractType.SoftwareSupply)
        {
            throw new InvalidOperationException(
                "Phase 8B chỉ hỗ trợ renderer cho hợp đồng cung cấp phần mềm.");
        }

        if (contract.IsLegacy)
        {
            throw new InvalidOperationException(
                "Hợp đồng legacy không có dữ liệu template để tạo preview động.");
        }
    }

    private static void EnsureTemplatePolicy(
        TblContractTemplateVersion version,
        TblContractTemplate template,
        TblContract contract)
    {
        if (template.DocumentType
            != (byte)TemplateDocumentType.SoftwareSupplyContract)
        {
            throw new InvalidOperationException(
                "Template của hợp đồng không phải mẫu cung cấp phần mềm.");
        }

        if (version.ValidationStatus != (byte)TemplateValidationStatus.Valid)
        {
            throw new InvalidOperationException(
                "Template của hợp đồng chưa vượt qua kiểm tra placeholder.");
        }

        if (version.Status is not ((byte)TemplateVersionStatus.Published)
            and not ((byte)TemplateVersionStatus.Retired))
        {
            throw new InvalidOperationException(
                "Template của hợp đồng chưa từng được publish.");
        }

        if (template.LanguageMode != contract.LanguageMode)
        {
            throw new InvalidOperationException(
                "Ngôn ngữ template không khớp với hợp đồng.");
        }
    }

    private async Task<byte[]> ReadTemplateSourceAsync(
        TblContractTemplateVersion templateVersion,
        CancellationToken cancellationToken)
    {
        var fileId = templateVersion.DocumentFileId
            ?? throw new InvalidOperationException(
                "Template chưa có DOCX nguồn để tạo preview.");
        var metadata = await _dbContext.TblFileStorages
            .AsNoTracking()
            .SingleOrDefaultAsync(file => file.FileId == fileId, cancellationToken);
        if (metadata is null
            || metadata.ObjectType != TemplateVersionObjectType
            || metadata.ObjectId != templateVersion.TemplateVersionId)
        {
            throw new InvalidOperationException(
                "Metadata DOCX nguồn không thuộc phiên bản template của hợp đồng.");
        }

        var storedDocument = await _fileStorageService.DownloadAsync(fileId);
        if (storedDocument is null)
        {
            throw new InvalidOperationException(
                "DOCX nguồn của template không còn khả dụng.");
        }

        await using var stream = storedDocument.Value.Stream;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (!string.IsNullOrWhiteSpace(templateVersion.DocumentHash))
        {
            var actualHash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!string.Equals(
                    actualHash,
                    templateVersion.DocumentHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "DOCX nguồn không khớp hash đã validation của template.");
            }
        }

        return bytes;
    }

    private static ContractTemplateRenderData CreateRenderData(
        SoftwareSupplyContractSnapshot snapshot,
        TblCustomer customer,
        IReadOnlyList<TblPaymentSchedule> paymentSchedules)
    {
        var contract = snapshot.Contract;
        var version = snapshot.Version;
        var customerSnapshot = snapshot.Customer;
        var tenant = snapshot.Tenant;
        var currency = version.CurrencyCode;

        var scalarValues = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CONTRACT_CODE"] = Required(contract.ContractCode, "Mã hợp đồng"),
            ["CONTRACT_NAME"] = Required(contract.ContractName, "Tên hợp đồng"),
            ["CONTRACT_NAME_EN"] = Value(contract.ContractNameEn),
            ["CONTRACT_DATE"] = FormatDate(contract.CreatedDate),
            ["EFFECTIVE_DATE"] = FormatRequiredDate(
                contract.EffectiveDate,
                "Ngày hiệu lực"),
            ["EXPIRE_DATE"] = FormatRequiredDate(
                contract.ExpireDate,
                "Ngày hết hạn"),
            ["CONTRACT_CURRENCY"] = currency,
            ["CONTRACT_TOTAL_AMOUNT"] = FormatAmount(version.TotalAmount),
            ["CONTRACT_TOTAL_AMOUNT_IN_WORDS"] =
                VietnameseMoneyTextFormatter.Format(version.TotalAmount, currency),
            ["CUSTOMER_CODE"] = Required(customer.CustomerCode, "Mã khách hàng"),
            ["CUSTOMER_NAME"] = customerSnapshot.RepresentativeName,
            ["CUSTOMER_REPRESENTATIVE_TITLE"] =
                customerSnapshot.RepresentativeTitle,
            ["CUSTOMER_COMPANY"] = Required(
                customer.CustomerCompany ?? customerSnapshot.LegalName,
                "Công ty khách hàng"),
            ["CUSTOMER_TAX_CODE"] = Required(
                customerSnapshot.TaxCode,
                "Mã số thuế khách hàng"),
            ["CUSTOMER_ADDRESS"] = customerSnapshot.Address,
            ["CUSTOMER_EMAIL"] = Required(customer.CustomerEmail, "Email khách hàng"),
            ["CUSTOMER_PHONE"] = Required(
                customerSnapshot.PhoneNumber,
                "Điện thoại khách hàng"),
            ["PROVIDER_LEGAL_NAME"] = tenant.LegalEntityName,
            ["PROVIDER_TAX_CODE"] = tenant.TaxCode,
            ["PROVIDER_ADDRESS"] = tenant.Address,
            ["PROVIDER_REPRESENTATIVE_NAME"] = tenant.RepresentativeName,
            ["PROVIDER_REPRESENTATIVE_TITLE"] = tenant.RepresentativeTitle,
            ["CUSTOMER_FAX"] = Value(customerSnapshot.FaxNumber),
            ["CUSTOMER_BANK_ACCOUNT_NUMBER"] =
                Value(customerSnapshot.BankAccountNumber),
            ["CUSTOMER_BANK_NAME"] = Value(customerSnapshot.BankName),
            ["PROVIDER_PHONE"] = Value(snapshot.Tenant.PhoneNumber),
            ["PROVIDER_FAX"] = Value(snapshot.Tenant.FaxNumber),
            ["PROVIDER_BANK_ACCOUNT_NUMBER"] =
                Value(snapshot.Tenant.BankAccountNumber),
            ["PROVIDER_BANK_NAME"] = Value(snapshot.Tenant.BankName),
            ["CUSTOMER_WEBSITE"] = Value(customer.CustomerWebsite),
            ["CUSTOMER_CITY"] = Value(customer.CustomerCity),
            ["CUSTOMER_COUNTRY"] = Value(customer.CustomerCountry)
        };

        var items = snapshot.Items.Select((item, index) =>
            new ContractTemplateRenderItem(
                index + 1,
                item.ItemType == (byte)ContractItemType.Product
                    ? "Sản phẩm"
                    : "Dịch vụ",
                BuildItemDescription(item, contract.LanguageMode),
                item.Quantity,
                item.UnitPrice,
                item.DiscountMode == (byte)ContractItemDiscountMode.Percentage
                    ? $"{item.DiscountPercent:0.####}%"
                    : FormatMoney(item.DiscountAmount, currency),
                item.IsTaxable ? $"{item.VatPercent:0.####}%" : "0%",
                item.LineTotal))
            .ToArray();

        var terms = snapshot.Terms.Select((term, index) =>
            new ContractTemplateRenderTerm(
                index + 1,
                term.TermTitle,
                Value(term.TermTitleEn),
                Value(term.TermContent),
                Value(term.TermContentEn)))
            .ToArray();

        var payments = paymentSchedules.Select((payment, index) =>
        {
            var amount = Convert.ToDecimal(payment.Amount);
            var percent = version.TotalAmount > 0
                ? amount / version.TotalAmount * 100m
                : 0m;
            var dueCondition = string.IsNullOrWhiteSpace(payment.Note)
                ? $"Hạn thanh toán {payment.DueDate:dd/MM/yyyy}"
                : $"Hạn {payment.DueDate:dd/MM/yyyy} — {payment.Note.Trim()}";
            return new ContractTemplateRenderPayment(
                index + 1,
                $"Đợt {index + 1}",
                $"{percent:0.##}%",
                amount,
                dueCondition);
        }).ToArray();

        return new ContractTemplateRenderData(
            scalarValues,
            items,
            payments,
            terms,
            new ContractTemplateRenderSignature(
                "ĐẠI DIỆN BÊN CUNG CẤP",
                $"{tenant.LegalEntityName} — MST: {tenant.TaxCode} — "
                + $"{tenant.Address} — {tenant.RepresentativeName}, "
                + tenant.RepresentativeTitle),
            new ContractTemplateRenderSignature(
                "ĐẠI DIỆN BÊN KHÁCH HÀNG",
                $"{customerSnapshot.LegalName} — MST: "
                + $"{Value(customerSnapshot.TaxCode)} — {customerSnapshot.Address} — "
                + $"{customerSnapshot.RepresentativeName}, "
                + customerSnapshot.RepresentativeTitle),
            string.Empty,
            currency);
    }

    private static string BuildItemDescription(
        ContractItemLegalSnapshot item,
        byte languageMode)
    {
        var vietnamese = string.IsNullOrWhiteSpace(item.ItemDescription)
            ? item.ItemName
            : $"{item.ItemName} — {item.ItemDescription}";
        if (languageMode != (byte)ContractLanguageMode.Bilingual
            || string.IsNullOrWhiteSpace(item.ItemNameEn))
        {
            return vietnamese;
        }

        var english = string.IsNullOrWhiteSpace(item.ItemDescriptionEn)
            ? item.ItemNameEn
            : $"{item.ItemNameEn} — {item.ItemDescriptionEn}";
        return $"{vietnamese} / {english}";
    }

    private static string FormatDate(DateTime value) =>
        $"Ngày {value:dd} tháng {value:MM} năm {value:yyyy}";

    private static string FormatRequiredDate(DateTime? value, string fieldName) =>
        value.HasValue
            ? FormatDate(value.Value)
            : throw new InvalidOperationException($"{fieldName} chưa được cấu hình.");

    private static string FormatMoney(decimal value, string currency) =>
        $"{value:N0} {currency}".Replace(',', '.');

    private static string FormatAmount(decimal value) =>
        $"{value:N0}".Replace(',', '.');

    private static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} chưa được cấu hình.");
        }

        return value.Trim();
    }

    private static string Value(string? value) => value?.Trim() ?? string.Empty;

    private static string SafeFileName(string contractCode, int contractId)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var normalized = new string(contractCode
            .Select(character => invalid.Contains(character) ? '-' : character)
            .ToArray()).Trim().Trim('.');
        return string.IsNullOrWhiteSpace(normalized)
            ? $"contract-{contractId}"
            : normalized;
    }

    private sealed record RenderedContractDocx(
        byte[] Content,
        string SafeContractCode,
        SoftwareSupplyContractSnapshot Snapshot,
        int TemplateVersionId);
}

internal static class VietnameseMoneyTextFormatter
{
    private static readonly string[] Digits =
        ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];

    public static string Format(decimal amount, string currencyCode)
    {
        var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (rounded < 0 || rounded > long.MaxValue)
        {
            return $"{rounded:N0} {currencyCode}".Replace(',', '.');
        }

        var value = decimal.ToInt64(rounded);
        var number = value == 0 ? "Không" : Capitalize(ReadNumber(value));
        var unit = string.Equals(currencyCode, "VND", StringComparison.OrdinalIgnoreCase)
            ? "đồng chẵn"
            : currencyCode;
        return $"{number} {unit}";
    }

    private static string ReadNumber(long value)
    {
        var scales = new[] { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };
        var groups = new List<int>();
        while (value > 0)
        {
            groups.Add((int)(value % 1000));
            value /= 1000;
        }

        var parts = new List<string>();
        for (var index = groups.Count - 1; index >= 0; index--)
        {
            if (groups[index] == 0)
            {
                continue;
            }

            var forceHundreds = index < groups.Count - 1 && groups[index] < 100;
            parts.Add(ReadGroup(groups[index], forceHundreds));
            if (!string.IsNullOrEmpty(scales[index]))
            {
                parts.Add(scales[index]);
            }
        }

        return string.Join(' ', parts);
    }

    private static string ReadGroup(int value, bool forceHundreds)
    {
        var hundreds = value / 100;
        var tens = value % 100 / 10;
        var ones = value % 10;
        var parts = new List<string>();

        if (hundreds > 0 || forceHundreds)
        {
            parts.Add(Digits[hundreds]);
            parts.Add("trăm");
        }

        if (tens > 1)
        {
            parts.Add(Digits[tens]);
            parts.Add("mươi");
        }
        else if (tens == 1)
        {
            parts.Add("mười");
        }
        else if (ones > 0 && (hundreds > 0 || forceHundreds))
        {
            parts.Add("lẻ");
        }

        if (ones > 0)
        {
            parts.Add(ones switch
            {
                1 when tens > 1 => "mốt",
                4 when tens > 1 => "tư",
                5 when tens > 0 => "lăm",
                _ => Digits[ones]
            });
        }

        return string.Join(' ', parts);
    }

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
}
