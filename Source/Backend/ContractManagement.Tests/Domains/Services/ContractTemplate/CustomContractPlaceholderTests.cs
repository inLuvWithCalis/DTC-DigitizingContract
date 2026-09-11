using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ContractManagement.Tests.Domains.Services.ContractTemplate;

public sealed class CustomContractPlaceholderTests
{
    private static DbDtctechContext Context() => new(new DbContextOptionsBuilder<DbDtctechContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static readonly ContractPlaceholderSourceRegistry Registry = new();
    private static ContractPlaceholderCatalog Catalog(DbDtctechContext db, bool enabled = true) =>
        new(db, Registry, Options.Create(new CustomContractPlaceholderOptions { Enabled = enabled }));
    private static ContractPlaceholderDefinitionService Service(DbDtctechContext db) => new(db, Catalog(db), Registry);
    private static SaveContractPlaceholderRequest Request(string key = "CUSTOM_CONTACT") => new()
    { PlaceholderKey = key, FieldLabel = "Người liên hệ", SourceFieldKey = "customer.contact-name" };

    private static async Task SeedActor(DbDtctechContext db)
    {
        db.TblEmployees.Add(new() { EmployeeId = 1, EmployeeType = (byte)EmployeeType.AdminOfficer, Status = 1 });
        await db.SaveChangesAsync();
    }

    [Fact]
    public void Registry_ContainsOnlyExplicitBusinessFields()
    {
        var fields = Registry.GetAllFields();
        Assert.Equal(6, fields.Select(x => x.ModuleKey).Distinct().Count());
        Assert.Equal(fields.Count, fields.Select(x => x.SourceFieldKey).Distinct().Count());
        Assert.Contains(fields, x => x.SourceFieldKey == "department.name");
        Assert.Contains(fields, x => x.SourceFieldKey == "customer.contact-name");
        Assert.DoesNotContain(fields, x => x.SourceFieldKey.Contains("password") || x.SourceFieldKey.Contains("token") || x.SourceFieldKey.Contains("storage"));
        Assert.Throws<PlaceholderOperationException>(() => Registry.GetRequired("Employee.EmployeePassword"));
        Assert.Throws<ArgumentException>(() => new ContractPlaceholderSourceRegistry([
            new CustomerPlaceholderSourceProvider(), new CustomerPlaceholderSourceProvider()]));
    }

    [Theory]
    [InlineData("N0", "1,235")]
    [InlineData("N2", "1,234.50")]
    [InlineData("0.##", "1234.5")]
    public void NumberFormatting_IsDeterministic(string format, string expected) =>
        Assert.Equal(expected, Registry.Format("version.total-amount", 1234.5m, format, null));

    [Fact]
    public void Formatting_ValidatesBeforeApplyingFallback()
    {
        Assert.Equal("10/09/2026", Registry.Format("contract.effective-date", new DateTime(2026, 9, 10), null, null));
        Assert.Equal("fallback", Registry.Format("customer.contact-name", " ", null, "fallback"));
        Assert.Equal("", Registry.Format("customer.contact-name", null, null, null));
        Assert.Throws<PlaceholderOperationException>(() => Registry.Format("customer.contact-name", null, "N0", "fallback"));
    }

    [Theory]
    [InlineData("1_INVALID", "PlaceholderKeyInvalid")]
    [InlineData("BAD__KEY", "PlaceholderKeyInvalid")]
    [InlineData("BAD KEY", "PlaceholderKeyInvalid")]
    [InlineData("CUSTOMER_NAME", "SystemPlaceholderImmutable")]
    public async Task Create_RejectsInvalidOrReservedKeys(string key, string error)
    {
        await using var db = Context(); await SeedActor(db);
        Assert.Equal(error, (await Assert.ThrowsAsync<PlaceholderOperationException>(() => Service(db).SaveAsync(null, Request(key), 1, default))).Code);
        Assert.Empty(db.TblContractPlaceholderDefinitions);
        Assert.Empty(db.TblContractPlaceholderAudits);
    }

    [Fact]
    public async Task Create_NormalizesKey_AndAuditsWithoutDefaultValue()
    {
        await using var db = Context(); await SeedActor(db);
        var request = Request(" {{custom_contact}} "); request.DefaultValue = "PII must not enter audit";
        var created = await Service(db).SaveAsync(null, request, 1, default);
        Assert.Equal("CUSTOM_CONTACT", created.Key);
        Assert.False(created.IsRequired); Assert.False(created.IsSystem);
        Assert.Equal(TemplatePlaceholderMultiplicity.ZeroOrOne, created.Multiplicity);
        Assert.Equal(8, Convert.FromBase64String(created.RowVersion!).Length);
        var audit = await db.TblContractPlaceholderAudits.SingleAsync();
        Assert.DoesNotContain("PII", audit.NewValuesJson);
        audit.ActionType = "edited";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task FormerSystemRepresentativeTitle_CanBeCreatedAsCustom()
    {
        await using var db = Context(); await SeedActor(db);
        var request = Request("CUSTOMER_REPRESENTATIVE_TITLE");
        request.FieldLabel = "Chức danh người đại diện";
        request.SourceFieldKey = "customer.representative-title";

        var created = await Service(db).SaveAsync(null, request, 1, default);

        Assert.False(created.IsSystem);
        Assert.Equal("customer.representative-title", created.SourceFieldKey);
        var rendered = new ContractTemplatePreviewRenderer().RenderSample(
            Document(created.Key), ContractLanguageMode.Vietnamese,
            [.. ContractPlaceholderCatalog.SystemDefinitions, created],
            new Dictionary<string, string> { [created.Key] = "Giám đốc custom" });
        using var document = WordprocessingDocument.Open(new MemoryStream(rendered), false);
        Assert.Contains("Giám đốc custom", document.MainDocumentPart!.Document!.InnerText);
    }

    [Fact]
    public async Task Update_RejectsStaleVersion_AndNeverRenamesKey()
    {
        await using var db = Context(); await SeedActor(db);
        var service = Service(db);
        var created = await service.SaveAsync(null, Request(), 1, default);
        var update = Request(); update.RowVersion = created.RowVersion; update.SourceFieldKey = "customer.phone";
        var changed = await service.SaveAsync(created.Id, update, 1, default);
        Assert.NotEqual(created.RowVersion, changed.RowVersion);
        Assert.Equal("PlaceholderConcurrencyConflict", (await Assert.ThrowsAsync<PlaceholderOperationException>(() => service.SaveAsync(created.Id, update, 1, default))).Code);
        update.PlaceholderKey = "RENAMED"; update.RowVersion = changed.RowVersion;
        Assert.Equal("PlaceholderKeyInvalid", (await Assert.ThrowsAsync<PlaceholderOperationException>(() => service.SaveAsync(created.Id, update, 1, default))).Code);
    }

    [Fact]
    public async Task TenantIsolation_Authorization_AndFeatureFlag()
    {
        await using var a = Context(); await using var b = Context();
        await SeedActor(a); await SeedActor(b);
        var item = await Service(a).SaveAsync(null, Request(), 1, default);
        Assert.DoesNotContain(await Catalog(b).GetAsync(), x => x.Key == item.Key);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(b).UsageAsync(item.Id!.Value, 1, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(a).SaveAsync(null, Request("OTHER"), 2, default));
        var disabled = new ContractPlaceholderDefinitionService(a, Catalog(a, false), Registry);
        Assert.Equal("CustomPlaceholdersDisabled", (await Assert.ThrowsAsync<PlaceholderOperationException>(() => disabled.SaveAsync(null, Request("OTHER"), 1, default))).Code);
        Assert.DoesNotContain(await Catalog(a, false).GetAsync(), x => !x.IsSystem);
    }

    [Fact]
    public async Task Delete_RemovesUnusedDefinition_ButKeepsAuditTrail()
    {
        await using var db = Context(); await SeedActor(db);
        var service = Service(db);
        var created = await service.SaveAsync(null, Request(), 1, default);

        Assert.True(await service.DeleteAsync(created.Id!.Value, created.RowVersion!, 1, default));

        Assert.Empty(db.TblContractPlaceholderDefinitions);
        Assert.Contains(db.TblContractPlaceholderAudits,
            x => x.ActionType == "PlaceholderDefinitionDeleted" && x.PlaceholderKey == created.Key);
    }

    [Fact]
    public async Task Delete_RejectsDefinitionUsedByAnyTemplateVersion()
    {
        await using var db = Context(); await SeedActor(db);
        var service = Service(db);
        var created = await service.SaveAsync(null, Request(), 1, default);
        db.TblContractTemplateFields.Add(new()
        {
            TemplateVersionId = 20, PlaceholderKey = created.Key, FieldLabel = created.Label,
            DataSource = created.DataSource, SourceFieldKey = created.SourceFieldKey!,
            IsSystem = false, DataKind = 1, Multiplicity = 2
        });
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<PlaceholderOperationException>(() =>
            service.DeleteAsync(created.Id!.Value, created.RowVersion!, 1, default));

        Assert.Equal("PlaceholderInUse", error.Code);
        Assert.Single(db.TblContractPlaceholderDefinitions);
        Assert.DoesNotContain(db.TblContractPlaceholderAudits,
            x => x.ActionType == "PlaceholderDefinitionDeleted");
    }

    [Fact]
    public async Task Validator_HandlesCustomSplitTokens_DuplicateAndInactiveKeys()
    {
        await using var db = Context(); await SeedActor(db);
        var item = await Service(db).SaveAsync(null, Request(), 1, default);
        var validator = new ContractTemplateDocumentValidator(Catalog(db));
        var accepted = await validator.ValidateAsync(File(Document("CUSTOM_CONTACT", split: true)));
        Assert.True(accepted.IsCatalogValid, accepted.ValidationMessage);
        Assert.Contains(accepted.Definitions!, x => x.Key == item.Key && x.SourceFieldKey == "customer.contact-name");
        var duplicate = await validator.ValidateAsync(File(Document("CUSTOM_CONTACT", duplicate: true)));
        Assert.Contains("MultiplicityViolation:CUSTOM_CONTACT", duplicate.ValidationMessage);
        await Service(db).SetActiveAsync(item.Id!.Value, false, item.RowVersion!, 1, default);
        Assert.False((await validator.ValidateAsync(File(Document("CUSTOM_CONTACT")))).IsCatalogValid);
        Assert.Contains(await Catalog(db).GetAsync(true), x => x.Key == item.Key && !x.IsActive);
    }

    [Fact]
    public async Task SnapshotBindingAndSampleRender_SurviveDefinitionChanges()
    {
        await using var db = Context(); await SeedActor(db);
        var item = await Service(db).SaveAsync(null, Request(), 1, default);
        var bytes = Document(item.Key, split: true);
        var validation = await new ContractTemplateDocumentValidator(Catalog(db)).ValidateAsync(File(bytes));
        var hash = ContractPlaceholderCatalog.Fingerprint(validation.Definitions!);
        var update = Request(); update.SourceFieldKey = "customer.phone"; update.RowVersion = item.RowVersion;
        await Service(db).SaveAsync(item.Id, update, 1, default);
        Assert.Equal(hash, ContractPlaceholderCatalog.Fingerprint(validation.Definitions!));
        Assert.NotEqual(validation.CatalogRevision, ContractPlaceholderCatalog.Fingerprint(await Catalog(db).GetAsync()));
        var result = new ContractTemplatePreviewRenderer().RenderSample(bytes, ContractLanguageMode.Vietnamese,
            validation.Definitions!, new Dictionary<string, string> { [item.Key] = "A & B <test>" });
        using var doc = WordprocessingDocument.Open(new MemoryStream(result), false);
        Assert.Contains("A & B <test>", doc.MainDocumentPart!.Document!.InnerText);
        Assert.DoesNotContain("{{", doc.MainDocumentPart!.Document!.InnerText);
    }

    [Fact]
    public async Task ValueSnapshot_FreezesMasterData_RefreshesDraft_AndProtectsLockedVersion()
    {
        await using var db = Context(); await SeedActor(db);
        var item = await Service(db).SaveAsync(null, Request(), 1, default);
        db.TblCustomers.Add(new() { CustomerId = 10, CustomerContactPersonName = "Original" });
        db.TblContractTemplateFields.Add(new()
        {
            TemplateVersionId = 20, PlaceholderKey = item.Key, FieldLabel = item.Label, DataSource = item.DataSource,
            SourceFieldKey = item.SourceFieldKey!, IsSystem = false, DataKind = 1, Multiplicity = 2
        });
        await db.SaveChangesAsync();
        var contract = new TblContract { ContractId = 30, CustomerId = 10, EmployeeId = 1, TemplateVersionId = 20 };
        var version = new TblContractVersion { VersionId = 40, ContractId = 30, TemplateVersionId = 20 };
        var values = new ContractPlaceholderValueService(db, Registry);
        Assert.Equal("Original", (await values.CaptureAsync(contract, version))[item.Key]);
        Assert.Equal("Original", (await values.CaptureAsync(contract, version))[item.Key]);
        Assert.Single(db.TblContractVersionPlaceholderValues.Local);
        await db.SaveChangesAsync();
        (await db.TblCustomers.SingleAsync()).CustomerContactPersonName = "Changed";
        await db.SaveChangesAsync();
        Assert.Equal("Original", (await values.CaptureAsync(contract, version))[item.Key]);
        Assert.Equal("Changed", (await values.CaptureAsync(contract, version, refresh: true))[item.Key]);
        await db.SaveChangesAsync();
        version.IsLocked = true;
        (await db.TblCustomers.SingleAsync()).CustomerContactPersonName = "Later"; await db.SaveChangesAsync();
        Assert.Equal("Changed", (await values.CaptureAsync(contract, version, refresh: true))[item.Key]);
        version.VersionId = 41;
        await Assert.ThrowsAsync<PlaceholderOperationException>(() => values.CaptureAsync(contract, version));
    }

    private static IFormFile File(byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", "template.docx");
    private static byte[] Document(string customKey, bool split = false, bool duplicate = false)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var body = new W.Body();
            foreach (var definition in ContractPlaceholderCatalog.SystemDefinitions.Where(x => x.IsRequired))
                body.Append(new W.Paragraph(new W.Run(new W.Text("{{" + definition.Key + "}}"))));
            body.Append(split ? new W.Paragraph(new W.Run(new W.Text("{{CUSTOM_")), new W.Run(new W.Text("CONTACT}}")))
                : new W.Paragraph(new W.Run(new W.Text("{{" + customKey + "}}"))));
            if (duplicate) body.Append(new W.Paragraph(new W.Run(new W.Text("{{" + customKey + "}}"))));
            doc.AddMainDocumentPart().Document = new W.Document(body);
        }
        return stream.ToArray();
    }
}
