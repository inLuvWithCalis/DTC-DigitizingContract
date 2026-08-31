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
                TermContent = "Nội dung được sửa trong wizard.",
                IsNegotiable = false,
                DisplayOrder = 2
            },
            new CreateContractTermRequest
            {
                TermCode = "CUSTOM_1",
                TermTitle = "Điều khoản bổ sung",
                TermContent = "Nội dung bổ sung.",
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
        var contract = await context.TblContracts
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(2, versions.Count);
        Assert.True(versions[0].IsLocked);
        Assert.False(string.IsNullOrWhiteSpace(
            versions[0].SnapshotHash));
        Assert.Contains(
            "\"schemaVersion\":4",
            versions[0].SnapshotJson);
        Assert.False(versions[1].IsLocked);
        Assert.Equal(versions[0].VersionId,
            versions[1].SourceVersionId);
        Assert.Equal(versions[1].VersionId,
            contract.CurrentVersionId);
        Assert.Equal("SNAPSHOT-CODE", copiedItem.ItemCode);
        Assert.Equal("Snapshot product", copiedItem.ItemName);
        Assert.Equal(100m, copiedItem.LineTotal);
        Assert.Equal("GENERAL", copiedTerm.TermCode);
        Assert.Equal(100m, versions[1].TotalAmount);
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
        source.SnapshotJson = "{\"schemaVersion\":4}";
        source.SnapshotHash = new string('a', 64);
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
        Assert.Equal("{\"schemaVersion\":4}", persistedSource.SnapshotJson);
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
