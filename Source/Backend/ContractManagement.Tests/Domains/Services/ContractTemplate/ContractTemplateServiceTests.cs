using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContractManagement.Tests.Domains.Services.ContractTemplate;

public sealed class ContractTemplateServiceTests
{
    private const int AdminOfficerId = 101;
    private const int OtherEmployeeId = 102;
    private const int InactiveAdminOfficerId = 103;

    [Fact]
    public async Task AdminOfficerActive_CanCreateTemplateAndDraftV1()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);

        var result = await CreateService(context).CreateAsync(
            new CreateContractTemplateRequest
            {
                TemplateCode = "  SW-SUPPLY  ",
                TemplateName = "  Hợp đồng cung cấp  ",
                TemplateNameEn = "Software supply",
                LanguageMode = ContractLanguageMode.Bilingual,
                Description = "  Mô tả  ",
                InitialChangeNote = "  Khởi tạo  "
            },
            AdminOfficerId);

        Assert.Equal("SW-SUPPLY", result.TemplateCode);
        Assert.Equal("Hợp đồng cung cấp", result.TemplateName);
        Assert.Equal(TemplateDocumentType.SoftwareSupplyContract, result.DocumentType);
        Assert.True(result.IsActive);
        Assert.Null(result.CurrentPublishedVersionId);
        var version = Assert.Single(result.Versions);
        Assert.Equal(1, version.VersionNo);
        Assert.Equal(TemplateVersionStatus.Draft, version.Status);
        Assert.Equal(TemplateValidationStatus.NotValidated, version.ValidationStatus);
        Assert.Null(version.DocumentFileId);
        Assert.False(string.IsNullOrWhiteSpace(result.RowVersion));
        Assert.False(string.IsNullOrWhiteSpace(version.RowVersion));
    }

    [Theory]
    [InlineData(OtherEmployeeId)]
    [InlineData(InactiveAdminOfficerId)]
    public async Task NonAdminOfficerOrInactiveActor_IsRejected(int employeeId)
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService(context).CreateAsync(
                CreateRequest("AUTH-FAIL"),
                employeeId));
    }

    [Fact]
    public async Task DuplicateTemplateCode_IsRejected()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);

        await service.CreateAsync(CreateRequest("DUPLICATE"), AdminOfficerId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(CreateRequest("  DUPLICATE  "), AdminOfficerId));
        Assert.Equal(1, await context.TblContractTemplates.CountAsync());
    }

    [Fact]
    public async Task StaleTemplateRowVersion_IsRejectedWithoutOverwritingNewMetadata()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest("CONCURRENT"), AdminOfficerId);

        var first = await service.UpdateAsync(
            created.TemplateId,
            new UpdateContractTemplateRequest
            {
                TemplateName = "First",
                RowVersion = created.RowVersion
            },
            AdminOfficerId);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service.UpdateAsync(
                created.TemplateId,
                new UpdateContractTemplateRequest
                {
                    TemplateName = "Stale",
                    RowVersion = created.RowVersion
                },
                AdminOfficerId));

        var current = await service.GetAsync(created.TemplateId, AdminOfficerId);
        Assert.Equal("First", current.TemplateName);
        Assert.Equal(first.RowVersion, current.RowVersion);
    }

    [Fact]
    public async Task OnlyDraftVersion_CanMutateTerms()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var published = await SeedPublishedTemplateAsync(context);
        var draft = await service.CopyVersionAsync(
            published.TemplateVersionId,
            new CopyContractTemplateVersionRequest
            {
                RowVersion = Encode(published.RowVersion),
                ChangeNote = "Draft tiếp theo"
            },
            AdminOfficerId);

        var term = await service.AddTermAsync(
            draft.TemplateVersionId,
            new CreateContractTemplateTermRequest
            {
                TermCode = "NEW_TERM",
                TermTitle = "Điều khoản mới",
                DisplayOrder = 1,
                VersionRowVersion = draft.RowVersion
            },
            AdminOfficerId);

        Assert.Equal("NEW_TERM", term.TermCode);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddTermAsync(
                published.TemplateVersionId,
                new CreateContractTemplateTermRequest
                {
                    TermCode = "NO_MUTATION",
                    TermTitle = "Không được sửa",
                    DisplayOrder = 1,
                    VersionRowVersion = Encode(published.RowVersion)
                },
                AdminOfficerId));
    }

    [Fact]
    public async Task CopyRequiresCurrentPublishedAndCopiesTermsWithoutDocumentState()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var source = await SeedPublishedTemplateAsync(context);

        var copy = await service.CopyVersionAsync(
            source.TemplateVersionId,
            new CopyContractTemplateVersionRequest
            {
                RowVersion = Encode(source.RowVersion)
            },
            AdminOfficerId);

        Assert.Equal(2, copy.VersionNo);
        Assert.Equal(TemplateVersionStatus.Draft, copy.Status);
        Assert.Equal(TemplateValidationStatus.NotValidated, copy.ValidationStatus);
        Assert.Null(copy.DocumentFileId);
        Assert.Null(copy.DocumentHash);
        Assert.Equal("PAYMENT", Assert.Single(copy.Terms).TermCode);
        Assert.NotEqual(source.TemplateVersionId, copy.TemplateVersionId);
    }

    [Fact]
    public async Task ReorderRejectsMissingDuplicateAndStaleRows()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest("REORDER"), AdminOfficerId);
        var version = Assert.Single(created.Versions);
        var first = await service.AddTermAsync(
            version.TemplateVersionId,
            CreateTermRequest("ONE", 0, version.RowVersion),
            AdminOfficerId);
        var afterFirst = await service.GetVersionAsync(
            version.TemplateVersionId,
            AdminOfficerId);
        var second = await service.AddTermAsync(
            version.TemplateVersionId,
            CreateTermRequest("TWO", 1, afterFirst.RowVersion),
            AdminOfficerId);
        var current = await service.GetVersionAsync(
            version.TemplateVersionId,
            AdminOfficerId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ReorderTermsAsync(
                version.TemplateVersionId,
                new ReorderContractTemplateTermsRequest
                {
                    VersionRowVersion = current.RowVersion,
                    Terms =
                    [
                        new()
                        {
                            TermId = first.TemplateTermId,
                            RowVersion = first.RowVersion,
                            DisplayOrder = 1
                        }
                    ]
                },
                AdminOfficerId));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ReorderTermsAsync(
                version.TemplateVersionId,
                new ReorderContractTemplateTermsRequest
                {
                    VersionRowVersion = current.RowVersion,
                    Terms =
                    [
                        new()
                        {
                            TermId = first.TemplateTermId,
                            RowVersion = first.RowVersion,
                            DisplayOrder = 0
                        },
                        new()
                        {
                            TermId = second.TemplateTermId,
                            RowVersion = second.RowVersion,
                            DisplayOrder = 0
                        }
                    ]
                },
                AdminOfficerId));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service.ReorderTermsAsync(
                version.TemplateVersionId,
                new ReorderContractTemplateTermsRequest
                {
                    VersionRowVersion = version.RowVersion,
                    Terms =
                    [
                        new()
                        {
                            TermId = first.TemplateTermId,
                            RowVersion = first.RowVersion,
                            DisplayOrder = 1
                        },
                        new()
                        {
                            TermId = second.TemplateTermId,
                            RowVersion = second.RowVersion,
                            DisplayOrder = 0
                        }
                    ]
                },
                AdminOfficerId));
    }

    private static ContractTemplateService CreateService(
        DbDtctechContext context) => new(context);

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedEmployeesAsync(DbDtctechContext context)
    {
        context.TblEmployees.AddRange(
            new TblEmployee
            {
                EmployeeId = AdminOfficerId,
                EmployeeType = (byte)EmployeeType.AdminOfficer,
                Status = 1,
                EmployeeFullName = "Admin Officer"
            },
            new TblEmployee
            {
                EmployeeId = OtherEmployeeId,
                EmployeeType = (byte)EmployeeType.Manager,
                Status = 1,
                EmployeeFullName = "Manager"
            },
            new TblEmployee
            {
                EmployeeId = InactiveAdminOfficerId,
                EmployeeType = (byte)EmployeeType.AdminOfficer,
                Status = 0,
                EmployeeFullName = "Inactive Admin Officer"
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static CreateContractTemplateRequest CreateRequest(string code) => new()
    {
        TemplateCode = code,
        TemplateName = $"Template {code}",
        LanguageMode = ContractLanguageMode.Vietnamese
    };

    private static CreateContractTemplateTermRequest CreateTermRequest(
        string code,
        int displayOrder,
        string versionRowVersion) => new()
    {
        TermCode = code,
        TermTitle = code,
        DisplayOrder = displayOrder,
        VersionRowVersion = versionRowVersion
    };

    private static async Task<TblContractTemplateVersion>
        SeedPublishedTemplateAsync(DbDtctechContext context)
    {
        var now = DateTime.UtcNow;
        var template = new TblContractTemplate
        {
            TemplateId = 1,
            TemplateCode = "PUBLISHED",
            TemplateName = "Published",
            DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            IsActive = true,
            CurrentPublishedVersionId = 2,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [1, 1, 1, 1, 1, 1, 1, 1]
        };
        var version = new TblContractTemplateVersion
        {
            TemplateVersionId = 2,
            TemplateId = 1,
            VersionNo = 1,
            Status = (byte)TemplateVersionStatus.Published,
            ValidationStatus = (byte)TemplateValidationStatus.Valid,
            DocumentFileId = 99,
            DocumentHash = new string('a', 64),
            PublishedByEmployeeId = AdminOfficerId,
            PublishedDate = now,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [2, 2, 2, 2, 2, 2, 2, 2]
        };
        context.TblContractTemplates.Add(template);
        context.TblContractTemplateVersions.Add(version);
        context.TblContractTemplateTerms.Add(new TblContractTemplateTerm
        {
            TemplateTermId = 3,
            TemplateVersionId = 2,
            TermCode = "PAYMENT",
            TermTitle = "Payment",
            DisplayOrder = 0,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [3, 3, 3, 3, 3, 3, 3, 3]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return version;
    }

    private static string Encode(byte[] rowVersion) =>
        Convert.ToBase64String(rowVersion);
}
