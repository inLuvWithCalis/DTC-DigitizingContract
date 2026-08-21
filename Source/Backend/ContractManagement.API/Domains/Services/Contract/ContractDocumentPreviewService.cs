using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Sinh PDF từ template nguồn và snapshot hiện hành của một hợp đồng thật.
/// Không tái sử dụng published template preview vì artifact đó chứa dữ liệu mẫu cố định.
/// </summary>
public sealed class ContractDocumentPreviewService : IContractDocumentPreviewService
{
    private readonly DbDtctechContext _dbContext;
    private readonly IContractService _contractService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IContractTemplatePreviewRenderer _documentRenderer;
    private readonly IContractTemplatePdfRenderer _pdfRenderer;

    public ContractDocumentPreviewService(
        DbDtctechContext dbContext,
        IContractService contractService,
        IFileStorageService fileStorageService,
        IContractTemplatePreviewRenderer documentRenderer,
        IContractTemplatePdfRenderer pdfRenderer)
    {
        _dbContext = dbContext;
        _contractService = contractService;
        _fileStorageService = fileStorageService;
        _documentRenderer = documentRenderer;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<(byte[] Content, string FileName)> GeneratePdfAsync(
        int contractId,
        int employeeId,
        bool canReadTenant,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractService.GetDetailAsync(
            contractId,
            employeeId,
            canReadTenant);

        var templateVersionId = contract.CurrentVersion.TemplateVersionId
            ?? contract.TemplateVersionId
            ?? throw new InvalidOperationException(
                "Hợp đồng không có template để tạo bản xem trước.");

        var templateFileId = await _dbContext.TblContractTemplateVersions
            .AsNoTracking()
            .Where(version => version.TemplateVersionId == templateVersionId)
            .Select(version => version.DocumentFileId)
            .SingleOrDefaultAsync(cancellationToken);

        if (templateFileId is not > 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy DOCX nguồn của template hợp đồng.");
        }

        var storedDocument = await _fileStorageService.DownloadAsync(
            templateFileId.Value);
        if (storedDocument is null)
        {
            throw new InvalidOperationException(
                "DOCX nguồn của template không còn khả dụng.");
        }

        await using var sourceStream = storedDocument.Value.Stream;
        using var sourceBuffer = new MemoryStream();
        await sourceStream.CopyToAsync(sourceBuffer, cancellationToken);

        var customer = await _dbContext.TblCustomers
            .AsNoTracking()
            .SingleAsync(
                item => item.CustomerId == contract.Customer.CustomerId,
                cancellationToken);

        var renderData = CreateRenderData(contract, customer);
        var renderedDocx = _documentRenderer.Render(
            sourceBuffer.ToArray(),
            contract.LanguageMode,
            renderData);
        var pdf = await _pdfRenderer.ConvertPreviewToPdfAsync(
            renderedDocx,
            cancellationToken);

        var safeCode = string.IsNullOrWhiteSpace(contract.ContractCode)
            ? $"contract-{contract.ContractId}"
            : contract.ContractCode;
        return (pdf, $"{safeCode}-preview.pdf");
    }

    private static ContractTemplateRenderData CreateRenderData(
        ContractDetailResponse contract,
        TblCustomer customer)
    {
        var currency = contract.CurrencyCode;
        var scalarValues = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CONTRACT_CODE"] = Value(contract.ContractCode),
            ["CONTRACT_NAME"] = Value(contract.ContractName),
            ["CONTRACT_NAME_EN"] = Value(contract.ContractNameEn),
            ["CONTRACT_DATE"] = FormatDate(contract.SignDate ?? contract.CreatedDate),
            ["EFFECTIVE_DATE"] = FormatDate(contract.EffectiveDate),
            ["EXPIRE_DATE"] = FormatDate(contract.ExpireDate),
            ["CONTRACT_CURRENCY"] = currency,
            ["CONTRACT_TOTAL_AMOUNT"] = FormatMoney(contract.TotalPayment, currency),
            ["CONTRACT_TOTAL_AMOUNT_IN_WORDS"] = FormatMoney(contract.TotalPayment, currency),
            ["CUSTOMER_CODE"] = Value(customer.CustomerCode),
            ["CUSTOMER_NAME"] = Value(customer.CustomerFullName),
            ["CUSTOMER_COMPANY"] = Value(customer.CustomerCompany),
            ["CUSTOMER_TAX_CODE"] = Value(customer.CustomerTaxCode),
            ["CUSTOMER_ADDRESS"] = Value(customer.CustomerAddress),
            ["CUSTOMER_EMAIL"] = Value(customer.CustomerEmail),
            ["CUSTOMER_PHONE"] = Value(customer.CustomerMobile ?? customer.CustomerPhone),
            ["CUSTOMER_FAX"] = Value(customer.CustomerFaxNumber),
            ["CUSTOMER_WEBSITE"] = Value(customer.CustomerWebsite),
            ["CUSTOMER_CITY"] = Value(customer.CustomerCity),
            ["CUSTOMER_COUNTRY"] = Value(customer.CustomerCountry)
        };

        var items = contract.CurrentVersion.Items
            .OrderBy(item => item.DisplayOrder)
            .Select((item, index) => new ContractTemplateRenderItem(
                index + 1,
                item.ItemType == ContractItemType.Product ? "Sản phẩm" : "Dịch vụ",
                BuildItemDescription(item, contract.LanguageMode),
                item.Quantity,
                item.UnitPrice,
                item.DiscountMode == ContractItemDiscountMode.Percentage
                    ? $"{item.DiscountPercent:0.####}%"
                    : FormatMoney(item.DiscountAmount, currency),
                item.IsTaxable ? $"{item.VatPercent:0.####}%" : "0%",
                item.LineTotal))
            .ToList();

        var terms = contract.CurrentVersion.Terms
            .OrderBy(term => term.DisplayOrder)
            .Select((term, index) => new ContractTemplateRenderTerm(
                index + 1,
                term.TermTitle,
                Value(term.TermTitleEn),
                Value(term.TermContent),
                Value(term.TermContentEn)))
            .ToList();

        return new ContractTemplateRenderData(
            scalarValues,
            items,
            [],
            terms,
            new ContractTemplateRenderSignature(
                "ĐẠI DIỆN BÊN CUNG CẤP",
                Value(contract.ResponsibleEmployee.EmployeeFullName)),
            new ContractTemplateRenderSignature(
                "ĐẠI DIỆN BÊN KHÁCH HÀNG",
                Value(customer.CustomerFullName)),
            string.Empty);
    }

    private static string BuildItemDescription(
        ContractItemDetailResponse item,
        ContractLanguageMode languageMode)
    {
        var vietnamese = string.IsNullOrWhiteSpace(item.ItemDescription)
            ? item.ItemName
            : $"{item.ItemName} — {item.ItemDescription}";
        if (languageMode != ContractLanguageMode.Bilingual
            || string.IsNullOrWhiteSpace(item.ItemNameEn))
        {
            return vietnamese;
        }

        var english = string.IsNullOrWhiteSpace(item.ItemDescriptionEn)
            ? item.ItemNameEn
            : $"{item.ItemNameEn} — {item.ItemDescriptionEn}";
        return $"{vietnamese} / {english}";
    }

    private static string FormatDate(DateTime? value) => value.HasValue
        ? $"Ngày {value.Value:dd} tháng {value.Value:MM} năm {value.Value:yyyy}"
        : string.Empty;

    private static string FormatMoney(decimal value, string currency) =>
        $"{value:N0} {currency}".Replace(',', '.');

    private static string Value(string? value) => value?.Trim() ?? string.Empty;
}
