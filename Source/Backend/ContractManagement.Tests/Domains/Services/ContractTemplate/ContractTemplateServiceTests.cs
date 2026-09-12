using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static ContractManagement.Tests.ContractRichTextTestData;

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
                TemplateCode = "  sw-supply  ",
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
        var layout = await context.TblContractTemplateItemTableColumnLayouts
            .Where(column => column.TemplateVersionId == version.TemplateVersionId)
            .OrderBy(column => column.DisplayOrder)
            .ToListAsync();
        Assert.Equal(
            ContractTableLayoutPolicy.ItemColumnKeys,
            layout.Select(column => column.ColumnKey));
        Assert.Equal(
            ContractTableLayoutPolicy.DefaultItemColumnWidthsBps,
            layout.Select(column => (int)column.WidthBps));
    }

    [Fact]
    public async Task DraftItemTableLayout_CanBeUpdatedAndInvalidatesPreview()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("LAYOUT-UPDATE"), AdminOfficerId);
        var draft = Assert.Single(created.Versions);
        var entity = await context.TblContractTemplateVersions.SingleAsync(
            version => version.TemplateVersionId == draft.TemplateVersionId);
        entity.PreviewFileId = 987;
        await context.SaveChangesAsync();

        var updated = await service.UpdateItemTableLayoutAsync(
            draft.TemplateVersionId,
            new UpdateContractTemplateItemTableLayoutRequest
            {
                VersionRowVersion = draft.RowVersion,
                ColumnWidthsBps =
                    [700, 900, 2_900, 650, 1_300, 900, 650, 2_000]
            },
            AdminOfficerId);

        Assert.Equal(
            [700, 900, 2_900, 650, 1_300, 900, 650, 2_000],
            updated.ItemTableLayout.Select(column => column.WidthBps));
        Assert.Null(updated.PreviewFileId);
        Assert.NotEqual(draft.RowVersion, updated.RowVersion);
    }

    [Theory]
    [InlineData(600, 900, 3_000, 650, 1_300, 900, 650, 1_999)]
    [InlineData(200, 1_300, 3_000, 650, 1_300, 900, 650, 2_000)]
    public async Task ItemTableLayout_WithNonCanonicalWidths_IsRejected(
        int first,
        int second,
        int third,
        int fourth,
        int fifth,
        int sixth,
        int seventh,
        int eighth)
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("LAYOUT-INVALID"), AdminOfficerId);
        var draft = Assert.Single(created.Versions);

        var exception = await Assert.ThrowsAsync<ContractTemplatePreviewException>(
            () => service.UpdateItemTableLayoutAsync(
                draft.TemplateVersionId,
                new UpdateContractTemplateItemTableLayoutRequest
                {
                    VersionRowVersion = draft.RowVersion,
                    ColumnWidthsBps =
                        [first, second, third, fourth, fifth, sixth, seventh, eighth]
                },
                AdminOfficerId));

        Assert.Equal("ItemTableLayoutInvalid", exception.FailureCode);
    }

    [Fact]
    public async Task PublishedItemTableLayout_CannotBeUpdated()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var published = await SeedPublishedTemplateAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context).UpdateItemTableLayoutAsync(
                published.TemplateVersionId,
                new UpdateContractTemplateItemTableLayoutRequest
                {
                    VersionRowVersion = Encode(published.RowVersion),
                    ColumnWidthsBps = ContractTableLayoutPolicy
                        .DefaultItemColumnWidthsBps.ToList()
                },
                AdminOfficerId));
    }

    [Fact]
    public async Task MissingItemTableLayout_IsRejectedWithoutDefaultFallback()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var published = await SeedPublishedTemplateAsync(context);
        context.TblContractTemplateItemTableColumnLayouts.RemoveRange(
            context.TblContractTemplateItemTableColumnLayouts);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ContractTemplatePreviewException>(
            () => CreateService(context).GetVersionAsync(
                published.TemplateVersionId,
                AdminOfficerId));

        Assert.Equal("ItemTableLayoutInvalid", exception.FailureCode);
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

    [Theory]
    [InlineData("Mã mẫu 1")]
    [InlineData("INVALID CODE")]
    [InlineData("-INVALID")]
    [InlineData("INVALID?")]
    public async Task InvalidTechnicalTemplateCode_IsRejected(string templateCode)
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(context).CreateAsync(
                CreateRequest(templateCode),
                AdminOfficerId));

        Assert.Contains("chữ cái không dấu", exception.Message);
        Assert.Empty(context.TblContractTemplates);
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
        Assert.Equal("CIVIL_CODE", Assert.Single(copy.LegalBases).BasisCode);
        Assert.NotEqual(source.TemplateVersionId, copy.TemplateVersionId);
        Assert.Equal(
            ContractTableLayoutPolicy.DefaultItemColumnWidthsBps,
            copy.ItemTableLayout.Select(column => column.WidthBps));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CopyVersionAsync(
                source.TemplateVersionId,
                new CopyContractTemplateVersionRequest
                {
                    RowVersion = Encode(source.RowVersion)
                },
                AdminOfficerId));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(
            ContractTemplateErrorCodes.DraftVersionAlreadyExists,
            exception.Code);
    }

    [Fact]
    public async Task LatestRetiredWithoutCurrentPublished_CanCreateOneNewDraft()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        await SeedPublishedTemplateAsync(context);
        var source = await context.TblContractTemplateVersions
            .SingleAsync(version => version.TemplateVersionId == 2);
        var template = await context.TblContractTemplates
            .SingleAsync(item => item.TemplateId == source.TemplateId);
        source.Status = (byte)TemplateVersionStatus.Retired;
        template.CurrentPublishedVersionId = null;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var copy = await service.CopyVersionAsync(
            source.TemplateVersionId,
            new CopyContractTemplateVersionRequest
            {
                RowVersion = Encode(source.RowVersion),
                ChangeNote = "Khôi phục mẫu thành bản nháp mới"
            },
            AdminOfficerId);

        Assert.Equal(2, copy.VersionNo);
        Assert.Equal(TemplateVersionStatus.Draft, copy.Status);
        Assert.Equal(TemplateValidationStatus.NotValidated, copy.ValidationStatus);
        Assert.Null(copy.DocumentFileId);
        Assert.Null(copy.DocumentHash);
        Assert.Equal("PAYMENT", Assert.Single(copy.Terms).TermCode);
        Assert.Equal(
            TemplateVersionStatus.Retired,
            (TemplateVersionStatus)source.Status);
        Assert.Null(template.CurrentPublishedVersionId);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CopyVersionAsync(
                source.TemplateVersionId,
                new CopyContractTemplateVersionRequest
                {
                    RowVersion = Encode(source.RowVersion)
                },
                AdminOfficerId));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(
            ContractTemplateErrorCodes.DraftVersionAlreadyExists,
            exception.Code);
    }

    [Fact]
    public void Model_HasUniqueFilteredIndexForSingleDraftPerTemplate()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(
            typeof(TblContractTemplateVersion));
        var index = Assert.Single(
            entityType!.GetIndexes(),
            candidate => candidate.GetDatabaseName()
                == "UX_tbl_ContractTemplateVersion_OneDraftPerTemplate");

        Assert.True(index.IsUnique);
        Assert.Equal("[Status] = 0", index.GetFilter());
        Assert.Equal(
            nameof(TblContractTemplateVersion.TemplateId),
            Assert.Single(index.Properties).Name);
    }

    [Fact]
    public async Task OlderRetiredVersion_CannotCreateDraft()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        await SeedPublishedTemplateAsync(context);
        var source = await context.TblContractTemplateVersions
            .SingleAsync(version => version.TemplateVersionId == 2);
        var template = await context.TblContractTemplates
            .SingleAsync(item => item.TemplateId == source.TemplateId);
        source.Status = (byte)TemplateVersionStatus.Retired;
        template.CurrentPublishedVersionId = null;
        context.TblContractTemplateVersions.Add(new TblContractTemplateVersion
        {
            TemplateVersionId = 4,
            TemplateId = template.TemplateId,
            VersionNo = 2,
            Status = (byte)TemplateVersionStatus.Retired,
            ValidationStatus = (byte)TemplateValidationStatus.Valid,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [4, 4, 4, 4, 4, 4, 4, 4]
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context).CopyVersionAsync(
                source.TemplateVersionId,
                new CopyContractTemplateVersionRequest
                {
                    RowVersion = Encode(source.RowVersion)
                },
                AdminOfficerId));
    }

    [Fact]
    public async Task AvailableLookup_ReturnsOnlyActiveCurrentPublishedVersions()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        await SeedPublishedTemplateAsync(context);

        context.TblContractTemplates.AddRange(
            new TblContractTemplate
            {
                TemplateId = 10,
                TemplateCode = "INACTIVE",
                TemplateName = "Inactive template",
                DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
                LanguageMode = (byte)ContractLanguageMode.Vietnamese,
                IsActive = false,
                CurrentPublishedVersionId = 11,
                CreatedEmployeeId = AdminOfficerId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = [10, 10, 10, 10, 10, 10, 10, 10]
            },
            new TblContractTemplate
            {
                TemplateId = 20,
                TemplateCode = "STALE",
                TemplateName = "Stale current version",
                DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
                LanguageMode = (byte)ContractLanguageMode.Vietnamese,
                IsActive = true,
                CurrentPublishedVersionId = 21,
                CreatedEmployeeId = AdminOfficerId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = [20, 20, 20, 20, 20, 20, 20, 20]
            });
        context.TblContractTemplateVersions.AddRange(
            new TblContractTemplateVersion
            {
                TemplateVersionId = 11,
                TemplateId = 10,
                VersionNo = 1,
                Status = (byte)TemplateVersionStatus.Published,
                ValidationStatus = (byte)TemplateValidationStatus.Valid,
                CreatedEmployeeId = AdminOfficerId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = [11, 11, 11, 11, 11, 11, 11, 11]
            },
            new TblContractTemplateVersion
            {
                TemplateVersionId = 21,
                TemplateId = 20,
                VersionNo = 1,
                Status = (byte)TemplateVersionStatus.Retired,
                ValidationStatus = (byte)TemplateValidationStatus.Valid,
                CreatedEmployeeId = AdminOfficerId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = [21, 21, 21, 21, 21, 21, 21, 21]
            });
        await context.SaveChangesAsync();

        var result = await CreateService(context).ListAvailableAsync();

        var available = Assert.Single(result);
        Assert.Equal(1, available.TemplateId);
        Assert.Equal(2, available.TemplateVersionId);
        Assert.Equal("PUBLISHED", available.TemplateCode);

        var detail = await CreateService(context).GetAvailableAsync(
            available.TemplateVersionId);
        var term = Assert.Single(detail.Terms);
        Assert.Equal("PAYMENT", term.TermCode);

        var page = await CreateService(context).SearchAvailableAsync(
            new AvailableContractTemplateFilterRequest
            {
                Keyword = "PUBLISHED",
                DocumentType = TemplateDocumentType.SoftwareSupplyContract,
                Page = 1,
                PageSize = 10
            });
        Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService(context).GetAvailableAsync(21));
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

    [Fact]
    public async Task PaymentTerm_MilestonesCanBeCreatedAndReordered_ButKindCannotBeCleared()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest("PAYMENT-PLAN"),
            AdminOfficerId);
        var version = Assert.Single(created.Versions);
        var term = await service.AddTermAsync(version.TemplateVersionId,
            new CreateContractTemplateTermRequest
            {
                TermKind = ContractTermKind.Payment,
                TermCode = "PAYMENT",
                TermTitle = "Thanh toán",
                DisplayOrder = 1,
                VersionRowVersion = version.RowVersion
            }, AdminOfficerId);
        var afterTerm = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        await service.AddPaymentMilestoneAsync(version.TemplateVersionId,
            term.TemplateTermId, new CreateContractTemplatePaymentMilestoneRequest
            {
                MilestoneCode = "M1",
                TitleVi = "Đợt 1",
                PaymentPercent = 40m,
                DueAnchor = PaymentDueAnchor.ContractSigned,
                DueOffsetDays = 5,
                DayCountMode = PaymentDayCountMode.CalendarDays,
                DisplayOrder = 1,
                VersionRowVersion = afterTerm.RowVersion
            }, AdminOfficerId);
        var afterFirst = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        await service.AddPaymentMilestoneAsync(version.TemplateVersionId,
            term.TemplateTermId, new CreateContractTemplatePaymentMilestoneRequest
            {
                MilestoneCode = "M2",
                TitleVi = "Đợt 2",
                PaymentPercent = 60m,
                DueAnchor = PaymentDueAnchor.AcceptanceCompleted,
                DueOffsetDays = 3,
                DayCountMode = PaymentDayCountMode.BusinessDays,
                DisplayOrder = 2,
                VersionRowVersion = afterFirst.RowVersion
            }, AdminOfficerId);
        var beforeReorder = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        var first = beforeReorder.Terms.Single().PaymentMilestones
            .Single(item => item.MilestoneCode == "M1");
        var second = beforeReorder.Terms.Single().PaymentMilestones
            .Single(item => item.MilestoneCode == "M2");

        var reordered = await service.ReorderPaymentMilestonesAsync(
            version.TemplateVersionId, term.TemplateTermId,
            new ReorderContractTemplatePaymentMilestonesRequest
            {
                VersionRowVersion = beforeReorder.RowVersion,
                Milestones =
                [
                    new() { MilestoneId = second.TemplatePaymentMilestoneId, RowVersion = second.RowVersion, DisplayOrder = 1 },
                    new() { MilestoneId = first.TemplatePaymentMilestoneId, RowVersion = first.RowVersion, DisplayOrder = 2 }
                ]
            }, AdminOfficerId);
        Assert.Equal("M2", reordered.Terms.Single().PaymentMilestones[0].MilestoneCode);

        var currentTerm = reordered.Terms.Single();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateTermAsync(version.TemplateVersionId,
                currentTerm.TemplateTermId,
                new UpdateContractTemplateTermRequest
                {
                    TermKind = ContractTermKind.General,
                    TermCode = currentTerm.TermCode,
                    TermTitle = currentTerm.TermTitle,
                    IsNegotiable = currentTerm.IsNegotiable,
                    DisplayOrder = currentTerm.DisplayOrder,
                    RowVersion = currentTerm.RowVersion,
                    VersionRowVersion = reordered.RowVersion
                }, AdminOfficerId));
    }

    [Fact]
    public async Task PaymentMilestone_UpdateCannotLeavePreviousPaymentAsFirstMilestone()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest("PAYMENT-FIRST"),
            AdminOfficerId);
        var version = Assert.Single(created.Versions);
        var term = await service.AddTermAsync(version.TemplateVersionId,
            new CreateContractTemplateTermRequest
            {
                TermKind = ContractTermKind.Payment,
                TermCode = "PAYMENT",
                TermTitle = "Thanh toán",
                DisplayOrder = 1,
                VersionRowVersion = version.RowVersion
            }, AdminOfficerId);
        var afterTerm = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        await service.AddPaymentMilestoneAsync(version.TemplateVersionId,
            term.TemplateTermId, new CreateContractTemplatePaymentMilestoneRequest
            {
                MilestoneCode = "M1",
                TitleVi = "Đợt 1",
                PaymentPercent = 40m,
                DueAnchor = PaymentDueAnchor.ContractSigned,
                DueOffsetDays = 0,
                DayCountMode = PaymentDayCountMode.CalendarDays,
                DisplayOrder = 1,
                VersionRowVersion = afterTerm.RowVersion
            }, AdminOfficerId);
        var afterFirst = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        await service.AddPaymentMilestoneAsync(version.TemplateVersionId,
            term.TemplateTermId, new CreateContractTemplatePaymentMilestoneRequest
            {
                MilestoneCode = "M2",
                TitleVi = "Đợt 2",
                PaymentPercent = 60m,
                DueAnchor = PaymentDueAnchor.PreviousMilestonePaid,
                DueOffsetDays = 0,
                DayCountMode = PaymentDayCountMode.CalendarDays,
                DisplayOrder = 2,
                VersionRowVersion = afterFirst.RowVersion
            }, AdminOfficerId);
        var current = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        var first = current.Terms.Single().PaymentMilestones
            .Single(item => item.MilestoneCode == "M1");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdatePaymentMilestoneAsync(version.TemplateVersionId,
                term.TemplateTermId, first.TemplatePaymentMilestoneId,
                new UpdateContractTemplatePaymentMilestoneRequest
                {
                    MilestoneCode = first.MilestoneCode,
                    TitleVi = first.TitleVi,
                    PaymentPercent = first.PaymentPercent,
                    DueAnchor = first.DueAnchor,
                    DueOffsetDays = first.DueOffsetDays,
                    DayCountMode = first.DayCountMode,
                    DisplayOrder = 3,
                    RowVersion = first.RowVersion,
                    VersionRowVersion = current.RowVersion
                }, AdminOfficerId));
    }

    [Fact]
    public async Task DraftLegalBases_CanBeCreatedReorderedUpdatedAndDeleted()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest("LEGAL-BASES"), AdminOfficerId);
        var version = Assert.Single(created.Versions);

        await service.AddLegalBasisAsync(version.TemplateVersionId,
            new CreateContractTemplateLegalBasisRequest
            {
                BasisCode = "CIVIL_CODE",
                ContentVi = RichText("Căn cứ Bộ luật Dân sự."),
                DisplayOrder = 1,
                VersionRowVersion = version.RowVersion
            }, AdminOfficerId);
        var afterFirst = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        await service.AddLegalBasisAsync(version.TemplateVersionId,
            new CreateContractTemplateLegalBasisRequest
            {
                BasisCode = "COMMERCIAL_LAW",
                ContentVi = RichText("Căn cứ Luật Thương mại."),
                DisplayOrder = 2,
                VersionRowVersion = afterFirst.RowVersion
            }, AdminOfficerId);
        var beforeReorder = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        var first = beforeReorder.LegalBases.Single(x => x.BasisCode == "CIVIL_CODE");
        var second = beforeReorder.LegalBases.Single(x => x.BasisCode == "COMMERCIAL_LAW");

        var reordered = await service.ReorderLegalBasesAsync(version.TemplateVersionId,
            new ReorderContractTemplateLegalBasesRequest
            {
                VersionRowVersion = beforeReorder.RowVersion,
                LegalBases =
                [
                    new() { LegalBasisId = second.TemplateLegalBasisId, RowVersion = second.RowVersion, DisplayOrder = 1 },
                    new() { LegalBasisId = first.TemplateLegalBasisId, RowVersion = first.RowVersion, DisplayOrder = 2 }
                ]
            }, AdminOfficerId);
        Assert.Equal("COMMERCIAL_LAW", reordered.LegalBases[0].BasisCode);

        var civil = reordered.LegalBases.Single(x => x.BasisCode == "CIVIL_CODE");
        var updated = await service.UpdateLegalBasisAsync(version.TemplateVersionId,
            civil.TemplateLegalBasisId,
            new UpdateContractTemplateLegalBasisRequest
            {
                BasisCode = civil.BasisCode,
                ContentVi = RichText("Căn cứ Bộ luật Dân sự số 91/2015/QH13."),
                DisplayOrder = civil.DisplayOrder,
                RowVersion = civil.RowVersion,
                VersionRowVersion = reordered.RowVersion
            }, AdminOfficerId);
        Assert.Contains("91/2015/QH13", updated.ContentVi);

        var beforeDelete = await service.GetVersionAsync(version.TemplateVersionId,
            AdminOfficerId);
        var commercial = beforeDelete.LegalBases.Single(x => x.BasisCode == "COMMERCIAL_LAW");
        await service.DeleteLegalBasisAsync(version.TemplateVersionId,
            commercial.TemplateLegalBasisId,
            new DeleteContractTemplateLegalBasisRequest
            {
                RowVersion = commercial.RowVersion,
                VersionRowVersion = beforeDelete.RowVersion
            }, AdminOfficerId);
        Assert.Equal("CIVIL_CODE",
            Assert.Single((await service.GetVersionAsync(version.TemplateVersionId,
                AdminOfficerId)).LegalBases).BasisCode);
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
        context.TblContractTemplateItemTableColumnLayouts.AddRange(
            ContractTableLayoutPolicy.ItemColumnKeys.Select((columnKey, index) =>
                new TblContractTemplateItemTableColumnLayout
                {
                    TemplateVersionId = version.TemplateVersionId,
                    ColumnKey = columnKey,
                    DisplayOrder = checked((byte)index),
                    WidthBps = checked((short)ContractTableLayoutPolicy
                        .DefaultItemColumnWidthsBps[index]),
                    CreatedEmployeeId = AdminOfficerId,
                    CreatedDate = now
                }));
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
        context.TblContractTemplateLegalBases.Add(new TblContractTemplateLegalBasis
        {
            TemplateLegalBasisId = 4,
            TemplateVersionId = 2,
            BasisCode = "CIVIL_CODE",
            ContentVi = RichText("Căn cứ Bộ luật Dân sự."),
            DisplayOrder = 1,
            CreatedEmployeeId = AdminOfficerId,
            CreatedDate = now,
            RowVersion = [4, 4, 4, 4, 4, 4, 4, 4]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return version;
    }

    private static string Encode(byte[] rowVersion) =>
        Convert.ToBase64String(rowVersion);
}
