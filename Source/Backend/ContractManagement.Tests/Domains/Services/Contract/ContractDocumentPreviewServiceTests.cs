using System.Security.Cryptography;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class ContractDocumentPreviewServiceTests
{
    private const int ContractId = 8101;
    private const int OwnerId = 8102;
    private const int OtherEmployeeId = 8103;
    private const int VersionId = 8104;
    private const int TemplateId = 8105;
    private const int TemplateVersionId = 8106;
    private const int FileId = 8107;

    [Fact]
    public async Task Owner_GeneratesDocx_FromCurrentVersionAndLegalSnapshots()
    {
        await using var context = CreateContext();
        var source = CreateSourceDocument();
        await SeedAsync(context, source);
        var service = CreateService(context, source);

        var result = await service.GenerateDocxAsync(ContractId, OwnerId);

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.ContentType);
        Assert.Equal("HD-8B-001-preview.docx", result.FileName);
        using var stream = new MemoryStream(result.Content);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = string.Concat(document.MainDocumentPart!.Document!
            .Descendants<W.Text>()
            .Select(item => item.Text));
        Assert.Contains("HD-8B-001", text);
        Assert.Contains("Ngày 15 tháng 08 năm 2026", text);
        Assert.DoesNotContain("Ngày 20 tháng 08 năm 2026", text);
        Assert.Contains("CÔNG TY DTC", text);
        Assert.Contains("0107654321", text);
        Assert.Contains("TP. Hồ Chí Minh", text);
        Assert.Contains("Nguyễn Provider", text);
        Assert.Contains("CÔNG TY KHÁCH HÀNG", text);
        Assert.Contains("Trần Customer", text);
        Assert.Contains("Phần mềm quản lý hợp đồng", text);
        Assert.Contains("Phạm vi cung cấp", text);
        Assert.Contains("USD", text);
        Assert.DoesNotContain("{{", text);
        Assert.DoesNotContain("DỮ LIỆU MẪU", text);
    }

    [Fact]
    public async Task Pdf_ConvertsTheSameRealDataDocx_AndDoesNotPersistArtifact()
    {
        await using var context = CreateContext();
        var source = CreateSourceDocument();
        await SeedAsync(context, source);
        var pdfRenderer = new CapturingPdfRenderer();
        var service = CreateService(context, source, pdfRenderer);
        var fileCountBefore = await context.TblFileStorages.CountAsync();

        var result = await service.GeneratePdfAsync(ContractId, OwnerId);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("HD-8B-001-preview.pdf", result.FileName);
        Assert.True(result.Content.AsSpan().StartsWith("%PDF-"u8));
        Assert.NotNull(pdfRenderer.InputDocx);
        using var stream = new MemoryStream(pdfRenderer.InputDocx!);
        using var document = WordprocessingDocument.Open(stream, false);
        Assert.Contains("HD-8B-001", string.Concat(document.MainDocumentPart!
            .Document!.Descendants<W.Text>().Select(item => item.Text)));
        Assert.Equal(fileCountBefore, await context.TblFileStorages.CountAsync());
    }

    [Fact]
    public async Task NonOwner_CannotPreview_EvenWhenTheyCanReadTenantContracts()
    {
        await using var context = CreateContext();
        var source = CreateSourceDocument();
        await SeedAsync(context, source);
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = OtherEmployeeId,
            EmployeeType = (byte)EmployeeType.Manager,
            Status = 1,
            RowVersion = [1]
        });
        await context.SaveChangesAsync();
        var service = CreateService(context, source);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.GenerateDocxAsync(ContractId, OtherEmployeeId));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Negotiating)]
    public async Task Preview_AllowsEditableLifecycle(ContractStatus status)
    {
        await using var context = CreateContext();
        var source = CreateSourceDocument();
        await SeedAsync(context, source);
        var contract = await context.TblContracts.SingleAsync();
        contract.Status = (byte)status;
        await context.SaveChangesAsync();
        var service = CreateService(context, source);

        var result = await service.GenerateDocxAsync(ContractId, OwnerId);

        Assert.NotEmpty(result.Content);
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval, ContractType.SoftwareSupply)]
    [InlineData(ContractStatus.Negotiating, ContractType.SoftwareMaintenance)]
    public async Task Preview_RejectsUnsupportedLifecycleOrContractType(
        ContractStatus status,
        ContractType contractType)
    {
        await using var context = CreateContext();
        var source = CreateSourceDocument();
        await SeedAsync(context, source);
        var contract = await context.TblContracts.SingleAsync();
        contract.Status = (byte)status;
        contract.ContractType = (byte)contractType;
        await context.SaveChangesAsync();
        var service = CreateService(context, source);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateDocxAsync(ContractId, OwnerId));
    }

    private static ContractDocumentPreviewService CreateService(
        DbDtctechContext context,
        byte[] source,
        IContractTemplatePdfRenderer? pdfRenderer = null) => new(
        context,
        new ContractResourceAuthorizationService(context),
        new SourceFileStorage(source),
        new ContractTemplatePreviewRenderer(),
        pdfRenderer ?? new CapturingPdfRenderer());

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(builder => builder.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedAsync(DbDtctechContext context, byte[] source)
    {
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = 8201,
            EmployeeId = OwnerId,
            ContractType = (byte)ContractType.SoftwareSupply,
            TemplateVersionId = TemplateVersionId,
            CurrentVersionId = VersionId,
            ContractCode = "HD-8B-001",
            ContractName = "Hợp đồng cung cấp phần mềm",
            ContractNameEn = "Software Supply Contract",
            SignDate = new DateTime(2026, 8, 20),
            EffectiveDate = new DateTime(2026, 9, 1),
            ExpireDate = new DateTime(2027, 8, 31),
            Status = (byte)ContractStatus.Negotiating,
            TotalAmount = 1_080m,
            Subtotal = 1_000m,
            TotalDiscount = 0m,
            TotalVat = 80m,
            CurrencyCode = "USD",
            LanguageMode = (byte)ContractLanguageMode.Bilingual,
            CreatedEmployeeId = OwnerId,
            CreatedDate = new DateTime(2026, 8, 15),
            RowVersion = [1]
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = VersionId,
            ContractId = ContractId,
            VersionNo = 2,
            TemplateVersionId = TemplateVersionId,
            CurrencyCode = "USD",
            Subtotal = 1_000m,
            TotalVat = 80m,
            TotalAmount = 1_080m,
            CreatedEmployeeId = OwnerId,
            CreatedDate = new DateTime(2026, 8, 19),
            RowVersion = [1]
        });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = 8201,
            CustomerCode = "CUS-8B",
            CustomerFullName = "Trần Customer",
            CustomerCompany = "CÔNG TY KHÁCH HÀNG",
            CustomerTaxCode = "0101234567",
            CustomerAddress = "Hà Nội",
            CustomerEmail = "customer@example.com",
            CustomerMobile = "0901234567",
            CustomerRepresentativeName = "Trần Customer",
            CustomerRepresentativeTitle = "Giám đốc",
            CustomerCountry = "Việt Nam"
        });
        context.TblTenantLegalProfiles.Add(new TblTenantLegalProfile
        {
            TenantLegalProfileId = 1,
            LegalEntityName = "CÔNG TY DTC",
            TaxCode = "0107654321",
            Address = "TP. Hồ Chí Minh",
            RepresentativeName = "Nguyễn Provider",
            RepresentativeTitle = "Tổng giám đốc",
            CreatedByEmployeeId = OwnerId,
            UpdatedByEmployeeId = OwnerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = [1]
        });
        context.TblContractItems.Add(new TblContractItem
        {
            ContractItemId = 8301,
            ContractId = ContractId,
            VersionId = VersionId,
            ItemType = (byte)ContractItemType.Product,
            ItemName = "Phần mềm quản lý hợp đồng",
            ItemNameEn = "Contract Management Software",
            Quantity = 1,
            UnitPrice = 1_000m,
            LineSubtotal = 1_000m,
            DiscountMode = (byte)ContractItemDiscountMode.None,
            IsTaxable = true,
            VatPercent = 8m,
            VatAmount = 80m,
            LineTotal = 1_080m,
            DisplayOrder = 1,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1]
        });
        context.TblContractTerms.Add(new TblContractTerm
        {
            TermId = 8401,
            ContractId = ContractId,
            VersionId = VersionId,
            TermCode = "SCOPE",
            TermTitle = "Phạm vi cung cấp",
            TermTitleEn = "Scope of supply",
            TermContent = "Cung cấp phần mềm theo danh mục.",
            TermContentEn = "Supply software as listed.",
            IsNegotiable = true,
            DisplayOrder = 1,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1]
        });
        context.TblContractTemplates.Add(new TblContractTemplate
        {
            TemplateId = TemplateId,
            TemplateCode = "SS-V1",
            TemplateName = "Software Supply",
            DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
            LanguageMode = (byte)ContractLanguageMode.Bilingual,
            IsActive = true,
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1]
        });
        context.TblContractTemplateVersions.Add(new TblContractTemplateVersion
        {
            TemplateVersionId = TemplateVersionId,
            TemplateId = TemplateId,
            VersionNo = 1,
            Status = (byte)TemplateVersionStatus.Published,
            ValidationStatus = (byte)TemplateValidationStatus.Valid,
            DocumentFileId = FileId,
            DocumentHash = Convert.ToHexString(SHA256.HashData(source)),
            CreatedEmployeeId = OwnerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1]
        });
        context.TblFileStorages.Add(new TblFileStorage
        {
            FileId = FileId,
            ObjectType = "ContractTemplateVersion",
            ObjectId = TemplateVersionId,
            FileName = "software-supply.docx",
            FilePath = "/test/software-supply.docx",
            UploadedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static byte[] CreateSourceDocument()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   DocumentFormat.OpenXml.WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new W.Body();
            foreach (var definition in SoftwareSupplyPlaceholderCatalog.GetAll())
            {
                body.Append(new W.Paragraph(
                    new W.Run(new W.Text($"{{{{{definition.Key}}}}}"))));
            }

            mainPart.Document = new W.Document(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private sealed class SourceFileStorage(byte[] source) : IFileStorageService
    {
        public Task<(Stream Stream, string FileName)?> DownloadAsync(int fileId) =>
            Task.FromResult<(Stream, string)?>(
                fileId == FileId
                    ? (new MemoryStream(source, writable: false), "software-supply.docx")
                    : null);

        public Task<FileStorageResponse> UploadAsync(
            IFormFile file,
            string objectType,
            int objectId,
            int uploadedBy) => throw new NotSupportedException();

        public Task<List<FileStorageResponse>> GetByObjectAsync(
            string objectType,
            int objectId) => throw new NotSupportedException();

        public Task DeleteAsync(int fileId) => throw new NotSupportedException();

        public Task DeleteUploadedArtifactAsync(FileStorageResponse file) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingPdfRenderer : IContractTemplatePdfRenderer
    {
        public byte[]? InputDocx { get; private set; }

        public Task<byte[]> ConvertPreviewToPdfAsync(
            byte[] previewDocx,
            CancellationToken cancellationToken = default)
        {
            InputDocx = previewDocx;
            return Task.FromResult("%PDF-test"u8.ToArray());
        }
    }
}
