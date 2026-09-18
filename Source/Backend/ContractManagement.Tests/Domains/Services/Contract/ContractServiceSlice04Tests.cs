using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static ContractManagement.Tests.ContractRichTextTestData;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class ContractServiceSlice04Tests
{
    private const int TenantId = 904;
    private const int EmployeeId = 41;
    private const int OtherEmployeeId = 42;
    private const int CustomerId = 51;
    private const int TemplateId = 61;
    private const int TemplateVersionId = 62;
    private const int ProductId = 71;
    private const int ServiceId = 72;

    [Theory]
    [InlineData("VND", "1.005", "1", "1")]
    [InlineData("USD", "1.005", "1", "1.01")]
    public async Task Create_ShouldRoundByCurrency(
        string currencyCode,
        string quantity,
        string unitPrice,
        string expectedTotal)
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var request = CreateRequest(currencyCode,
        [
            CreateItem(
                ContractItemType.Product,
                decimal.Parse(quantity),
                decimal.Parse(unitPrice),
                sourceProductId: ProductId)
        ]);

        var result = await CreateService(context)
            .CreateAsync(request, EmployeeId);

        Assert.Equal(
            decimal.Parse(expectedTotal),
            result.TotalPayment);
        Assert.Equal(result.TotalPayment, result.TotalAmount);
    }

    [Fact]
    public async Task Create_ShouldPersistFourSnapshotTypesAndFinance()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var request = CreateRequest("VND",
        [
            CreateItem(
                ContractItemType.Product,
                1m,
                100.5m,
                sourceProductId: ProductId,
                discountMode:
                    ContractItemDiscountMode.Percentage,
                discountPercent: 10m,
                isTaxable: true,
                vatPercent: 0m),
            CreateItem(
                ContractItemType.Service,
                1m,
                200m,
                sourceServiceId: ServiceId,
                discountMode:
                    ContractItemDiscountMode.FixedAmount,
                fixedDiscountAmount: 20m,
                isTaxable: true,
                vatPercent: 10m),
            CreateItem(
                ContractItemType.Product,
                1m,
                10m,
                isTaxable: false),
            CreateItem(
                ContractItemType.Service,
                1m,
                5.5m,
                isTaxable: false)
        ]);

        var result = await CreateService(context)
            .CreateAsync(request, EmployeeId);

        var items = await context.TblContractItems
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        Assert.Equal(4, items.Count);
        Assert.Equal(ProductId, items[0].SourceProductId);
        Assert.Equal("P-01", items[0].ItemCode);
        Assert.Equal(ServiceId, items[1].SourceServiceId);
        Assert.Null(items[1].ItemCode);
        Assert.Null(items[2].SourceProductId);
        Assert.Null(items[2].SourceServiceId);
        Assert.Null(items[3].SourceProductId);
        Assert.Null(items[3].SourceServiceId);

        Assert.True(items[0].IsTaxable);
        Assert.Equal(0m, items[0].VatPercent);
        Assert.False(items[2].IsTaxable);
        Assert.Equal(0m, items[2].VatPercent);

        Assert.Equal(317m, result.Subtotal);
        Assert.Equal(30m, result.TotalDiscount);
        Assert.Equal(18m, result.TotalVat);
        Assert.Equal(305m, result.TotalPayment);

        var version = await context.TblContractVersions
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(result.Subtotal, version.Subtotal);
        Assert.Equal(result.TotalPayment, version.TotalAmount);
        var legalBasis = await context.TblContractLegalBases
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal("CIVIL_CODE", legalBasis.BasisCode);
        Assert.Equal(64, legalBasis.SourceTemplateLegalBasisId);
    }

    [Fact]
    public async Task Create_ShouldPersistEditedAndCustomTermsAtomically()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        var request = CreateRequest("VND",
        [
            CreateItem(
                ContractItemType.Product,
                1m,
                100m,
                sourceProductId: ProductId)
        ]);
        request.Terms =
        [
            new CreateContractTermRequest
            {
                SourceTemplateTermId = 63,
                TermCode = "GENERAL",
                TermTitle = "Điều khoản chung đã sửa",
                TermContent = RichText("Nội dung được sửa trong wizard."),
                IsNegotiable = false,
                DisplayOrder = 2
            },
            new CreateContractTermRequest
            {
                TermCode = "CUSTOM_1",
                TermTitle = "Điều khoản bổ sung",
                TermContent = RichText("Nội dung bổ sung."),
                IsNegotiable = true,
                DisplayOrder = 1
            }
        ];

        var result = await CreateService(context)
            .CreateAsync(request, EmployeeId);

        var terms = await context.TblContractTerms
            .AsNoTracking()
            .OrderBy(term => term.DisplayOrder)
            .ToListAsync();
        Assert.Equal(2, result.TermCount);
        Assert.Equal("CUSTOM_1", terms[0].TermCode);
        Assert.Null(terms[0].SourceTemplateTermId);
        Assert.Equal("Điều khoản chung đã sửa", terms[1].TermTitle);
        Assert.Equal(63, terms[1].SourceTemplateTermId);
        Assert.False(terms[1].IsNegotiable);
    }

    [Fact]
    public async Task Create_ShouldSnapshotPaymentMilestones_AndBalanceLastAmount()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        var templateTerm = await context.TblContractTemplateTerms.SingleAsync();
        templateTerm.TermKind = (byte)ContractTermKind.Payment;
        context.TblContractTemplatePaymentMilestones.AddRange(
            new TblContractTemplatePaymentMilestone
            {
                TemplateVersionId = TemplateVersionId,
                TemplateTermId = templateTerm.TemplateTermId,
                MilestoneCode = "M1",
                TitleVi = "Đợt 1",
                PaymentPercent = 33.3333m,
                DueAnchor = (byte)PaymentDueAnchor.ContractEffectiveDate,
                DueOffsetDays = 2,
                DayCountMode = (byte)PaymentDayCountMode.CalendarDays,
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            },
            new TblContractTemplatePaymentMilestone
            {
                TemplateVersionId = TemplateVersionId,
                TemplateTermId = templateTerm.TemplateTermId,
                MilestoneCode = "M2",
                TitleVi = "Đợt 2",
                PaymentPercent = 66.6667m,
                DueAnchor = (byte)PaymentDueAnchor.ManualDate,
                DueOffsetDays = 5,
                DayCountMode = (byte)PaymentDayCountMode.BusinessDays,
                DisplayOrder = 2,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var effectiveDate = new DateTime(2026, 9, 10);
        var request = CreateRequest("VND",
            [CreateItem(ContractItemType.Product, 1m, 101m,
                sourceProductId: ProductId)]);
        request.EffectiveDate = effectiveDate;
        var manualMilestoneId = await context.TblContractTemplatePaymentMilestones
            .Where(item => item.MilestoneCode == "M2")
            .Select(item => item.TemplatePaymentMilestoneId)
            .SingleAsync();
        request.PaymentMilestoneDates =
        [
            new ContractPaymentMilestoneDateRequest
            {
                SourceTemplatePaymentMilestoneId = manualMilestoneId,
                AnchorDate = new DateTime(2026, 9, 4)
            }
        ];

        await CreateService(context).CreateAsync(request, EmployeeId);

        var term = await context.TblContractTerms.AsNoTracking().SingleAsync();
        var milestones = await context.TblContractPaymentMilestones.AsNoTracking()
            .OrderBy(item => item.DisplayOrder).ToListAsync();
        Assert.Equal((byte)ContractTermKind.Payment, term.TermKind);
        Assert.Equal([34m, 67m], milestones.Select(item => item.Amount));
        Assert.Equal(101m, milestones.Sum(item => item.Amount));
        Assert.All(milestones, item => Assert.Equal(term.TermId, item.TermId));
        Assert.Equal(effectiveDate, milestones[0].AnchorDate);
        Assert.Equal(effectiveDate.AddDays(2), milestones[0].DueDate);
        Assert.Equal(new DateTime(2026, 9, 4), milestones[1].AnchorDate);
        Assert.Equal(new DateTime(2026, 9, 11), milestones[1].DueDate);
    }

    [Fact]
    public async Task Create_ShouldRejectTermFromAnotherTemplate()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        var request = CreateRequest("VND",
        [
            CreateItem(
                ContractItemType.Product,
                1m,
                100m,
                sourceProductId: ProductId)
        ]);
        request.Terms =
        [
            new CreateContractTermRequest
            {
                SourceTemplateTermId = 99999,
                TermCode = "FOREIGN",
                TermTitle = "Không hợp lệ",
                IsNegotiable = true,
                DisplayOrder = 1
            }
        ];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(context).CreateAsync(request, EmployeeId));

        Assert.Empty(context.TblContracts);
    }

    [Fact]
    public async Task Create_ShouldCopyRequiredAndSelectedOptionalAppendicesWithTerms()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await SeedTemplateAppendicesAsync(context);
        var request = CreateRequest("VND",
            [CreateItem(ContractItemType.Product, 1m, 100m,
                sourceProductId: ProductId)]);
        request.SelectedOptionalTemplateAppendixIds = [66];

        var response = await CreateService(context).CreateAsync(request, EmployeeId);

        var appendices = await context.TblContractAppendices.AsNoTracking()
            .OrderBy(item => item.DisplayOrder).ToListAsync();
        var terms = await context.TblContractAppendixTerms.AsNoTracking()
            .OrderBy(item => item.AppendixId).ToListAsync();
        Assert.Equal(2, response.AppendixCount);
        Assert.Equal([65, 66], appendices.Select(item => item.SourceTemplateAppendixId));
        Assert.Equal(2, terms.Count);
        Assert.All(appendices, item =>
            Assert.Equal(response.CurrentVersionId, item.VersionId));
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(65)]
    public async Task Create_InvalidOptionalAppendixSelection_DoesNotPersistPartialContract(
        int selectedId)
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await SeedTemplateAppendicesAsync(context);
        var request = CreateRequest("VND",
            [CreateItem(ContractItemType.Product, 1m, 100m,
                sourceProductId: ProductId)]);
        request.SelectedOptionalTemplateAppendixIds = [selectedId];

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            CreateService(context).CreateAsync(request, EmployeeId));

        Assert.Empty(context.TblContracts);
        Assert.Empty(context.TblContractAppendices);
        Assert.Empty(context.TblContractAppendixTerms);
    }

    [Fact]
    public async Task Create_DuplicateOptionalAppendixSelection_IsRejected()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await SeedTemplateAppendicesAsync(context);
        var request = CreateRequest("VND",
            [CreateItem(ContractItemType.Product, 1m, 100m,
                sourceProductId: ProductId)]);
        request.SelectedOptionalTemplateAppendixIds = [66, 66];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(context).CreateAsync(request, EmployeeId));

        Assert.Empty(context.TblContracts);
        Assert.Empty(context.TblContractAppendices);
    }

    [Fact]
    public async Task Create_ShouldRejectMixedDiscountModes()
    {
        await using var context = CreateContext();

        var item = CreateItem(
            ContractItemType.Product,
            1m,
            100m,
            discountMode:
                ContractItemDiscountMode.Percentage,
            discountPercent: 10m,
            fixedDiscountAmount: 5m);

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService(context).CreateAsync(
                CreateRequest("VND", [item]),
                EmployeeId));

        Assert.Empty(context.TblContracts);
    }

    [Fact]
    public async Task NegotiationRound_ShouldCopySnapshotsAndLockSource()
    {
        await using var context = CreateContext();
        var sourceVersionId =
            await SeedNegotiatingContractAsync(context);

        var sourceTerm = await context.TblContractTerms.SingleAsync();
        sourceTerm.TermKind = (byte)ContractTermKind.Payment;
        context.TblContractPaymentMilestones.Add(new TblContractPaymentMilestone
        {
            ContractId = 100,
            VersionId = sourceVersionId,
            TermId = sourceTerm.TermId,
            MilestoneCode = "M1",
            TitleVi = "Đợt nghiệm thu",
            PaymentPercent = 100m,
            DueAnchor = (byte)PaymentDueAnchor.AcceptanceCompleted,
            DueOffsetDays = 3,
            DayCountMode = (byte)PaymentDayCountMode.CalendarDays,
            DisplayOrder = 1,
            Amount = 100m,
            AnchorDate = new DateTime(2026, 9, 1),
            DueDate = new DateTime(2026, 9, 4),
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractAppendices.Add(new TblContractAppendix
        {
            AppendixId = 105,
            ContractId = 100,
            VersionId = sourceVersionId,
            SourceTemplateAppendixId = 65,
            AppendixCode = "PL-01",
            AppendixName = "Phụ lục nguồn",
            IsRequired = true,
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractAppendixTerms.Add(new TblContractAppendixTerm
        {
            AppendixTermId = 106,
            AppendixId = 105,
            SourceTemplateAppendixTermId = 67,
            TermCode = "PL_TERM",
            TermTitle = "Điều khoản phụ lục nguồn",
            TermContent = RichText("Nội dung nguồn"),
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });

        context.TblProducts.Add(new TblProduct
        {
            ProductId = ProductId,
            ProductCode = "CAT-CHANGED",
            ProductName = "Catalog changed",
            Status = 0
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var response = await CreateService(context)
            .CreateNegotiationRoundAsync(
                100,
                CreateRoundRequest(sourceVersionId),
                EmployeeId);

        var versions = await context.TblContractVersions
            .AsNoTracking()
            .OrderBy(x => x.VersionNo)
            .ToListAsync();
        var copiedItem = await context.TblContractItems
            .AsNoTracking()
            .SingleAsync(x =>
                x.VersionId == response.CurrentVersion.VersionId);
        var copiedTerm = await context.TblContractTerms
            .AsNoTracking()
            .SingleAsync(x =>
                x.VersionId == response.CurrentVersion.VersionId);
        var copiedLegalBasis = await context.TblContractLegalBases
            .AsNoTracking()
            .SingleAsync(x => x.VersionId == response.CurrentVersion.VersionId);
        var copiedMilestone = await context.TblContractPaymentMilestones
            .AsNoTracking()
            .SingleAsync(x => x.VersionId == response.CurrentVersion.VersionId);
        var copiedAppendix = await context.TblContractAppendices.AsNoTracking()
            .SingleAsync(x => x.VersionId == response.CurrentVersion.VersionId);
        var copiedAppendixTerm = await context.TblContractAppendixTerms
            .AsNoTracking()
            .SingleAsync(x => x.AppendixId == copiedAppendix.AppendixId);
        var contract = await context.TblContracts
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(2, versions.Count);
        Assert.True(versions[0].IsLocked);
        Assert.False(string.IsNullOrWhiteSpace(
            versions[0].SnapshotHash));
        var relationalSnapshot = await context.TblContractVersionLegalSnapshots
            .Include(snapshot => snapshot.Parties)
            .Include(snapshot => snapshot.PaymentMilestones)
            .SingleAsync(snapshot => snapshot.VersionId == versions[0].VersionId);
        Assert.Equal(2, relationalSnapshot.Parties.Count);
        Assert.False(versions[1].IsLocked);
        Assert.Equal(versions[0].VersionId,
            versions[1].SourceVersionId);
        Assert.Equal(versions[1].VersionId,
            contract.CurrentVersionId);
        Assert.Equal("SNAPSHOT-CODE", copiedItem.ItemCode);
        Assert.Equal("Snapshot product", copiedItem.ItemName);
        Assert.Equal(100m, copiedItem.LineTotal);
        Assert.Equal("GENERAL", copiedTerm.TermCode);
        Assert.Equal("CIVIL_CODE", copiedLegalBasis.BasisCode);
        Assert.NotEqual(105, copiedAppendix.AppendixId);
        Assert.Equal(65, copiedAppendix.SourceTemplateAppendixId);
        Assert.NotEqual(106, copiedAppendixTerm.AppendixTermId);
        Assert.Equal(RichText("Nội dung nguồn"),
            copiedAppendixTerm.TermContent);
        Assert.Null(copiedMilestone.AnchorDate);
        Assert.Null(copiedMilestone.DueDate);
        Assert.Equal((byte)ContractPaymentMilestoneStatus.Unpaid,
            copiedMilestone.PaymentStatus);
        Assert.Null(copiedMilestone.PaidAt);
        Assert.Null(copiedMilestone.PaidByEmployeeId);
        Assert.Single(relationalSnapshot.PaymentMilestones);
        Assert.Equal(100m, versions[1].TotalAmount);
    }

    [Fact]
    public async Task NegotiationRound_WithPaidStructuredMilestone_IsRejected()
    {
        await using var context = CreateContext();
        var sourceVersionId = await SeedNegotiatingContractAsync(context);
        var sourceTerm = await context.TblContractTerms.SingleAsync();
        sourceTerm.TermKind = (byte)ContractTermKind.Payment;
        context.TblContractPaymentMilestones.Add(new TblContractPaymentMilestone
        {
            ContractId = 100,
            VersionId = sourceVersionId,
            TermId = sourceTerm.TermId,
            MilestoneCode = "M1",
            TitleVi = "Đợt đã nộp",
            PaymentPercent = 100m,
            DueAnchor = (byte)PaymentDueAnchor.ContractEffectiveDate,
            DueOffsetDays = 0,
            DayCountMode = (byte)PaymentDayCountMode.CalendarDays,
            DisplayOrder = 1,
            Amount = 100m,
            PaymentStatus = (byte)ContractPaymentMilestoneStatus.Paid,
            PaidAt = DateTime.UtcNow,
            PaidByEmployeeId = EmployeeId,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context).CreateNegotiationRoundAsync(
                100,
                CreateRoundRequest(sourceVersionId),
                EmployeeId));

        Assert.Contains("đã ghi nhận thanh toán", exception.Message);
        Assert.Single(await context.TblContractVersions.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AppendixMutation_LockedVersion_IsRejectedBeforeWriting()
    {
        await using var context = CreateContext();
        var versionId = await SeedNegotiatingContractAsync(context);
        var version = await context.TblContractVersions.SingleAsync();
        context.Entry(version).State = EntityState.Unchanged;
        version.IsLocked = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context).AddAppendixAsync(
                100,
                versionId,
                new AddContractAppendixRequest
                {
                    TemplateAppendixId = 66,
                    VersionRowVersion = Convert.ToBase64String(
                        InitialRowVersion())
                },
                EmployeeId));

        Assert.Empty(context.TblContractAppendices);
        Assert.Empty(context.TblContractAppendixTerms);
    }

    [Fact]
    public async Task AppendixTerms_EditableVersion_SupportsCrudAndReorder()
    {
        await using var context = CreateContext();
        var versionId = await SeedNegotiatingContractAsync(context);
        context.TblContractAppendices.Add(new TblContractAppendix
        {
            AppendixId = 105,
            ContractId = 100,
            VersionId = versionId,
            SourceTemplateAppendixId = 65,
            AppendixCode = "PL-01",
            AppendixName = "Phụ lục runtime",
            IsRequired = true,
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractAppendixTerms.AddRange(
            new TblContractAppendixTerm
            {
                AppendixTermId = 106,
                AppendixId = 105,
                TermCode = "TERM-1",
                TermTitle = "Điều một",
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            },
            new TblContractAppendixTerm
            {
                AppendixTermId = 107,
                AppendixId = 105,
                TermCode = "TERM-2",
                TermTitle = "Điều hai",
                DisplayOrder = 2,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var rowVersion = Convert.ToBase64String(InitialRowVersion());
        var service = CreateService(context);

        var updated = await service.UpdateAppendixTermAsync(
            100, versionId, 105, 106,
            new UpdateContractAppendixTermRequest
            {
                TermCode = "TERM-1",
                TermTitle = "Điều một đã sửa",
                TermContent = RichText("Nội dung đã sửa"),
                DisplayOrder = 1,
                RowVersion = rowVersion,
                AppendixRowVersion = rowVersion,
                VersionRowVersion = rowVersion
            }, EmployeeId);
        Assert.Equal("Điều một đã sửa", updated.TermTitle);

        var reordered = await service.ReorderAppendixTermsAsync(
            100, versionId, 105,
            new ReorderContractAppendixTermsRequest
            {
                AppendixRowVersion = rowVersion,
                VersionRowVersion = rowVersion,
                Terms =
                [
                    new ReorderContractAppendixTermItem
                    {
                        AppendixTermId = 107,
                        RowVersion = rowVersion,
                        DisplayOrder = 1
                    },
                    new ReorderContractAppendixTermItem
                    {
                        AppendixTermId = 106,
                        RowVersion = rowVersion,
                        DisplayOrder = 2
                    }
                ]
            }, EmployeeId);
        Assert.Equal([107, 106],
            reordered.Terms.Select(item => item.AppendixTermId));

        await service.DeleteAppendixTermAsync(
            100, versionId, 105, 107,
            new DeleteContractAppendixTermRequest
            {
                RowVersion = rowVersion,
                AppendixRowVersion = rowVersion,
                VersionRowVersion = rowVersion
            }, EmployeeId);

        var added = await service.AddAppendixTermAsync(
            100, versionId, 105,
            new CreateContractAppendixTermRequest
            {
                TermCode = "TERM-3",
                TermTitle = "Điều ba",
                DisplayOrder = 3,
                AppendixRowVersion = rowVersion,
                VersionRowVersion = rowVersion
            }, EmployeeId);

        Assert.Equal("TERM-3", added.TermCode);
        Assert.Equal(2, await context.TblContractAppendixTerms.CountAsync());
        var actions = await context.TblContractAudits.AsNoTracking()
            .Select(item => item.ActionType).ToListAsync();
        Assert.Contains(ContractAuditActionTypes.ContractAppendixTermUpdated,
            actions);
        Assert.Contains(ContractAuditActionTypes.ContractAppendixTermsReordered,
            actions);
        Assert.Contains(ContractAuditActionTypes.ContractAppendixTermDeleted,
            actions);
        Assert.Contains(ContractAuditActionTypes.ContractAppendixTermCreated,
            actions);
    }

    [Fact]
    public async Task NegotiationRound_ShouldRejectUnauthorizedActor()
    {
        await using var context = CreateContext();
        var sourceVersionId =
            await SeedNegotiatingContractAsync(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService(context)
                .CreateNegotiationRoundAsync(
                    100,
                    CreateRoundRequest(sourceVersionId),
                    OtherEmployeeId));

        Assert.Single(context.TblContractVersions);
    }

    [Fact]
    public async Task NegotiationRound_StaleRequestShouldNotCreatePartialRows()
    {
        await using var context = CreateContext();
        var sourceVersionId =
            await SeedNegotiatingContractAsync(context);
        var request = CreateRoundRequest(sourceVersionId);
        request.RowVersion =
            Convert.ToBase64String([9, 9, 9, 9, 9, 9, 9, 9]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => CreateService(context)
                .CreateNegotiationRoundAsync(
                    100,
                    request,
                    EmployeeId));

        Assert.Single(context.TblContractVersions);
        Assert.Single(context.TblContractItems);
        Assert.Single(context.TblContractTerms);
        var audit = Assert.Single(context.TblContractAudits);
        Assert.Equal(
            ContractAuditActionTypes.NegotiationRoundCreated,
            audit.ActionType);
        Assert.Equal(ContractAuditResults.ConcurrencyConflict, audit.Result);
        Assert.Equal(ContractAuditFailureCodes.StaleRowVersion, audit.FailureCode);
    }

    [Theory]
    [InlineData(ApprovalRequestStatus.Returned, ContractStatus.Negotiating)]
    [InlineData(ApprovalRequestStatus.Withdrawn, ContractStatus.Negotiating)]
    [InlineData(ApprovalRequestStatus.Rejected, ContractStatus.Rejected)]
    public async Task NegotiationRound_AfterApprovalDecision_BranchesLockedVersion(
        ApprovalRequestStatus approvalStatus,
        ContractStatus contractStatus)
    {
        await using var context = CreateContext();
        var sourceVersionId = await SeedNegotiatingContractAsync(context);
        var contract = await context.TblContracts.SingleAsync();
        var source = await context.TblContractVersions.SingleAsync();
        contract.Status = (byte)contractStatus;
        source.IsLocked = true;
        source.LockedDate = DateTime.UtcNow.AddMinutes(-5);
        source.LockedByEmployeeId = EmployeeId;
        source.SnapshotHash = new string('a', 64);
        context.TblContractVersionLegalSnapshots.Add(
            ContractManagement.API.Domains.Models.Contract
                .SoftwareSupplyContractSnapshotFactory.CreatePersistenceGraph(
                    ContractSnapshotTestData.Create(
                        contract.ContractId,
                        source.VersionId,
                        source.TemplateVersionId),
                    EmployeeId,
                    DateTime.UtcNow));
        context.TblContractApprovalRequests.Add(
            new TblContractApprovalRequest
            {
                ApprovalRequestId = 700,
                ContractId = contract.ContractId,
                VersionId = source.VersionId,
                Status = (byte)approvalStatus,
                SubmittedByEmployeeId = EmployeeId,
                SubmittedDate = DateTime.UtcNow.AddMinutes(-10),
                ResolvedByEmployeeId = OtherEmployeeId,
                ResolvedDate = DateTime.UtcNow.AddMinutes(-5),
                RowVersion = InitialRowVersion()
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var response = await CreateService(context)
            .CreateNegotiationRoundAsync(
                contract.ContractId,
                CreateRoundRequest(sourceVersionId),
                EmployeeId);

        var persistedSource = await context.TblContractVersions
            .AsNoTracking()
            .SingleAsync(version => version.VersionId == sourceVersionId);
        var persistedContract = await context.TblContracts
            .AsNoTracking()
            .SingleAsync();
        Assert.True(persistedSource.IsLocked);
        Assert.NotNull(persistedSource.SnapshotHash);
        Assert.False(response.CurrentVersion.IsLocked);
        Assert.Equal(ContractStatus.Negotiating, response.Status);
        Assert.Equal((byte)ContractStatus.Negotiating, persistedContract.Status);
        Assert.Equal(sourceVersionId, response.CurrentVersion.SourceVersionId);
    }

    private static DbDtctechContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<DbDtctechContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString(),
                    databaseOptions =>
                        databaseOptions.EnableNullChecks(false))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(
                        InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        return new DbDtctechContext(options);
    }

    private static ContractService CreateService(
        DbDtctechContext context)
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "TENANT-904",
            "Tenant 904",
            TenantDatabaseMode.Dedicated,
            "InMemory"));

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "slice-04-test"
            }
        };

        return new ContractService(
            context,
            new ContractAuditWriter(context, tenant, accessor));
    }

    private static async Task SeedCreateDependenciesAsync(
        DbDtctechContext context)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = EmployeeId,
            EmployeeAccount = "slice04",
            EmployeeFullName = "Slice 04",
            EmployeeType = (byte)EmployeeType.Sale,
            Status = 1
        });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Demo customer",
            CustomerCompany = "Demo Company",
            CustomerAddress = "Ha Noi",
            CustomerRepresentativeName = "Demo Representative",
            CustomerRepresentativeTitle = "Director",
            Status = 1
        });
        context.TblTenantLegalProfiles.Add(new TblTenantLegalProfile
        {
            TenantLegalProfileId = 1,
            LegalEntityName = "DTC Company",
            TaxCode = "0100000001",
            Address = "Ho Chi Minh City",
            RepresentativeName = "Provider Representative",
            RepresentativeTitle = "General Director",
            CreatedByEmployeeId = EmployeeId,
            UpdatedByEmployeeId = EmployeeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblProducts.Add(new TblProduct
        {
            ProductId = ProductId,
            ProductCode = "P-01",
            ProductName = "Product",
            Status = 1
        });
        context.TblServices.Add(new TblService
        {
            ServiceId = ServiceId,
            ServiceName = "Service",
            Status = 1
        });
        context.TblContractTemplates.Add(new TblContractTemplate
        {
            TemplateId = TemplateId,
            TemplateCode = "TPL-S04",
            TemplateName = "Slice 04",
            DocumentType =
                (byte)TemplateDocumentType
                    .SoftwareSupplyContract,
            LanguageMode =
                (byte)ContractLanguageMode.Vietnamese,
            CurrentPublishedVersionId = TemplateVersionId,
            IsActive = true,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = []
        });
        context.TblContractTemplateVersions.Add(
            new TblContractTemplateVersion
            {
                TemplateVersionId = TemplateVersionId,
                TemplateId = TemplateId,
                VersionNo = 1,
                Status = (byte)TemplateVersionStatus.Published,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });
        context.TblContractTemplateTerms.Add(
            new TblContractTemplateTerm
            {
                TemplateTermId = 63,
                TemplateVersionId = TemplateVersionId,
                TermCode = "GENERAL",
                TermTitle = "General",
                IsNegotiable = true,
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });
        context.TblContractTemplateLegalBases.Add(
            new TblContractTemplateLegalBasis
            {
                TemplateLegalBasisId = 64,
                TemplateVersionId = TemplateVersionId,
                BasisCode = "CIVIL_CODE",
                ContentVi = RichText("Căn cứ Bộ luật Dân sự."),
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task SeedTemplateAppendicesAsync(
        DbDtctechContext context)
    {
        context.TblContractTemplateAppendices.AddRange(
            new TblContractTemplateAppendix
            {
                TemplateAppendixId = 65,
                TemplateVersionId = TemplateVersionId,
                AppendixCode = "PL-REQ",
                AppendixName = "Phụ lục bắt buộc",
                IsRequired = true,
                IsSelectedByDefault = true,
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            },
            new TblContractTemplateAppendix
            {
                TemplateAppendixId = 66,
                TemplateVersionId = TemplateVersionId,
                AppendixCode = "PL-OPT",
                AppendixName = "Phụ lục tùy chọn",
                IsRequired = false,
                IsSelectedByDefault = false,
                DisplayOrder = 2,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            });
        context.TblContractTemplateAppendixTerms.AddRange(
            new TblContractTemplateAppendixTerm
            {
                TemplateAppendixTermId = 67,
                TemplateAppendixId = 65,
                TermCode = "REQ-01",
                TermTitle = "Điều khoản bắt buộc",
                TermContent = RichText("Nội dung bắt buộc"),
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            },
            new TblContractTemplateAppendixTerm
            {
                TemplateAppendixTermId = 68,
                TemplateAppendixId = 66,
                TermCode = "OPT-01",
                TermTitle = "Điều khoản tùy chọn",
                TermContent = RichText("Nội dung tùy chọn"),
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = InitialRowVersion()
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static CreateContractRequest CreateRequest(
        string currencyCode,
        List<CreateContractItemRequest> items)
    {
        return new CreateContractRequest
        {
            CustomerId = CustomerId,
            ContractType = ContractType.SoftwareSupply,
            TemplateVersionId = TemplateVersionId,
            ContractName = "Slice 04 demo",
            CurrencyCode = currencyCode,
            LanguageMode = ContractLanguageMode.Vietnamese,
            Items = items
        };
    }

    private static CreateContractItemRequest CreateItem(
        ContractItemType itemType,
        decimal quantity,
        decimal unitPrice,
        int? sourceProductId = null,
        int? sourceServiceId = null,
        ContractItemDiscountMode discountMode =
            ContractItemDiscountMode.None,
        decimal discountPercent = 0m,
        decimal fixedDiscountAmount = 0m,
        bool isTaxable = true,
        decimal vatPercent = 0m)
    {
        return new CreateContractItemRequest
        {
            ItemType = itemType,
            SourceProductId = sourceProductId,
            SourceServiceId = sourceServiceId,
            ItemCode = sourceProductId.HasValue
                ? "SNAPSHOT-CODE"
                : null,
            ItemName = $"{itemType} snapshot",
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountMode = discountMode,
            DiscountPercent = discountPercent,
            FixedDiscountAmount = fixedDiscountAmount,
            IsTaxable = isTaxable,
            VatPercent = vatPercent
        };
    }

    private static async Task<int> SeedNegotiatingContractAsync(
        DbDtctechContext context)
    {
        var rowVersion = InitialRowVersion();

        context.TblEmployees.AddRange(
            new TblEmployee
            {
                EmployeeId = EmployeeId,
                EmployeeAccount = "responsible",
                EmployeeFullName = "Responsible",
                EmployeeType = (byte)EmployeeType.Sale,
                Status = 1
            },
            new TblEmployee
            {
                EmployeeId = OtherEmployeeId,
                EmployeeAccount = "other",
                EmployeeFullName = "Other",
                EmployeeType = (byte)EmployeeType.Technical,
                Status = 1
            });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Demo customer",
            CustomerCompany = "Demo Company",
            CustomerAddress = "Ha Noi",
            CustomerRepresentativeName = "Demo Representative",
            CustomerRepresentativeTitle = "Director",
            Status = 1
        });
        context.TblTenantLegalProfiles.Add(new TblTenantLegalProfile
        {
            TenantLegalProfileId = 1,
            LegalEntityName = "DTC Company",
            TaxCode = "0100000001",
            Address = "Ho Chi Minh City",
            RepresentativeName = "Provider Representative",
            RepresentativeTitle = "General Director",
            CreatedByEmployeeId = EmployeeId,
            UpdatedByEmployeeId = EmployeeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = rowVersion
        });

        var contract = new TblContract
        {
            ContractId = 100,
            CustomerId = CustomerId,
            EmployeeId = EmployeeId,
            ContractType = (byte)ContractType.SoftwareSupply,
            CurrentVersionId = 101,
            ContractCode = "HD-S04",
            ContractName = "Slice 04",
            Status = (byte)ContractStatus.Negotiating,
            CurrencyCode = "VND",
            Subtotal = 100m,
            TotalDiscount = 10m,
            TotalVat = 10m,
            TotalAmount = 100m,
            LanguageMode =
                (byte)ContractLanguageMode.Vietnamese,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        };
        var version = new TblContractVersion
        {
            VersionId = 101,
            ContractId = contract.ContractId,
            VersionNo = 1,
            CurrencyCode = "VND",
            Subtotal = 100m,
            TotalDiscount = 10m,
            TotalVat = 10m,
            TotalAmount = 100m,
            IsLocked = false,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        };

        context.TblContracts.Add(contract);
        context.TblContractVersions.Add(version);
        context.TblContractItems.Add(new TblContractItem
        {
            ContractItemId = 102,
            ContractId = contract.ContractId,
            VersionId = version.VersionId,
            ItemType = (byte)ContractItemType.Product,
            SourceProductId = ProductId,
            ItemCode = "SNAPSHOT-CODE",
            ItemName = "Snapshot product",
            Quantity = 1m,
            UnitPrice = 100m,
            LineSubtotal = 100m,
            DiscountMode =
                (byte)ContractItemDiscountMode.Percentage,
            DiscountPercent = 10m,
            DiscountAmount = 10m,
            IsTaxable = true,
            VatPercent = 10m,
            VatAmount = 10m,
            LineTotal = 100m,
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        });
        context.TblContractTerms.Add(new TblContractTerm
        {
            TermId = 103,
            ContractId = contract.ContractId,
            VersionId = version.VersionId,
            TermCode = "GENERAL",
            TermTitle = "General",
            IsNegotiable = true,
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        });
        context.TblContractLegalBases.Add(new TblContractLegalBasis
        {
            LegalBasisId = 104,
            ContractId = contract.ContractId,
            VersionId = version.VersionId,
            BasisCode = "CIVIL_CODE",
            ContentVi = RichText("Căn cứ Bộ luật Dân sự."),
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return version.VersionId;
    }

    private static CreateContractNegotiationRoundRequest
        CreateRoundRequest(int currentVersionId)
    {
        var rowVersion =
            Convert.ToBase64String(InitialRowVersion());

        return new CreateContractNegotiationRoundRequest
        {
            CurrentVersionId = currentVersionId,
            RowVersion = rowVersion,
            CurrentVersionRowVersion = rowVersion,
            ChangeNote = "Round 2"
        };
    }

    private static byte[] InitialRowVersion() =>
        [1, 2, 3, 4, 5, 6, 7, 8];
}
