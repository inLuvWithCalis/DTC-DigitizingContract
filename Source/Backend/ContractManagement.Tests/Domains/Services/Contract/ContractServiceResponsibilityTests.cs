using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Tests.Domains.Services.Contract;

public class ContractServiceResponsibilityTests
{
    private const int CreatorEmployeeId = 101;
    private const int ResponsibleEmployeeId = 202;
    private const int CustomerId = 301;
    private const int TemplateId = 401;
    private const int TemplateVersionId = 402;

    [Fact]
    public async Task CreateAsync_ShouldDefaultResponsibilityToCreator()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var service = new ContractService(context);
        var response = await service.CreateAsync(
            CreateRequest(),
            CreatorEmployeeId);

        var contract = await context.TblContracts.SingleAsync();

        Assert.Equal(CreatorEmployeeId, response.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.CreatedEmployeeId);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseSelectedActiveEmployee()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await AddEmployeeAsync(
            context,
            ResponsibleEmployeeId,
            status: 1,
            EmployeeType.Technical);

        var service = new ContractService(context);
        var response = await service.CreateAsync(
            CreateRequest(ResponsibleEmployeeId),
            CreatorEmployeeId);

        var contract = await context.TblContracts.SingleAsync();

        Assert.Equal(ResponsibleEmployeeId, response.EmployeeId);
        Assert.Equal(ResponsibleEmployeeId, contract.EmployeeId);
        Assert.Equal(CreatorEmployeeId, contract.CreatedEmployeeId);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingResponsibleEmployee()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);

        var service = new ContractService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                CreateRequest(ResponsibleEmployeeId),
                CreatorEmployeeId));

        Assert.Empty(context.TblContracts);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactiveResponsibleEmployee()
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await AddEmployeeAsync(
            context,
            ResponsibleEmployeeId,
            status: 0,
            EmployeeType.Marketing);

        var service = new ContractService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(
                CreateRequest(ResponsibleEmployeeId),
                CreatorEmployeeId));

        Assert.Empty(context.TblContracts);
    }

    [Theory]
    [InlineData(EmployeeType.Sale)]
    [InlineData(EmployeeType.Marketing)]
    [InlineData(EmployeeType.AdminOfficer)]
    [InlineData(EmployeeType.Technical)]
    [InlineData(EmployeeType.Accountant)]
    [InlineData(EmployeeType.Manager)]
    public async Task CreateAsync_ShouldAllowEveryEmployeeType(
        EmployeeType employeeType)
    {
        await using var context = CreateContext();
        await SeedCreateDependenciesAsync(context);
        await AddEmployeeAsync(
            context,
            ResponsibleEmployeeId,
            status: 1,
            employeeType);

        var service = new ContractService(context);
        var response = await service.CreateAsync(
            CreateRequest(ResponsibleEmployeeId),
            CreatorEmployeeId);

        Assert.Equal(ResponsibleEmployeeId, response.EmployeeId);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotResolveEmployeeFromAnotherTenantContext()
    {
        await using var currentTenantContext = CreateContext();
        await SeedCreateDependenciesAsync(currentTenantContext);

        await using var otherTenantContext = CreateContext();
        await AddEmployeeAsync(
            otherTenantContext,
            ResponsibleEmployeeId,
            status: 1,
            EmployeeType.Manager);

        var service = new ContractService(currentTenantContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                CreateRequest(ResponsibleEmployeeId),
                CreatorEmployeeId));

        Assert.Empty(currentTenantContext.TblContracts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResponsibleEmployeeId_ShouldRejectNonPositiveValue(
        int responsibleEmployeeId)
    {
        var request = CreateRequest(responsibleEmployeeId);
        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(request)
        {
            MemberName = nameof(CreateContractRequest.ResponsibleEmployeeId)
        };

        var isValid = Validator.TryValidateProperty(
            request.ResponsibleEmployeeId,
            validationContext,
            results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
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

    private static async Task SeedCreateDependenciesAsync(
        DbDtctechContext context)
    {
        await AddEmployeeAsync(
            context,
            CreatorEmployeeId,
            status: 1,
            EmployeeType.Sale);

        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Khách hàng kiểm thử",
            Status = 1
        });

        context.TblContractTemplates.Add(new TblContractTemplate
        {
            TemplateId = TemplateId,
            TemplateCode = "TEST_SOFTWARE_SUPPLY",
            TemplateName = "Template kiểm thử",
            DocumentType =
                (byte)TemplateDocumentType.SoftwareSupplyContract,
            LanguageMode = (byte)ContractLanguageMode.Vietnamese,
            CurrentPublishedVersionId = TemplateVersionId,
            IsActive = true,
            CreatedEmployeeId = CreatorEmployeeId,
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
                ValidationStatus =
                    (byte)TemplateValidationStatus.Valid,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });

        context.TblContractTemplateTerms.Add(
            new TblContractTemplateTerm
            {
                TemplateTermId = 501,
                TemplateVersionId = TemplateVersionId,
                TermCode = "GENERAL",
                TermTitle = "Điều khoản chung",
                TermContent = "Nội dung kiểm thử",
                IsNegotiable = true,
                DisplayOrder = 1,
                CreatedEmployeeId = CreatorEmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = []
            });

        await context.SaveChangesAsync();
    }

    private static async Task AddEmployeeAsync(
        DbDtctechContext context,
        int employeeId,
        byte status,
        EmployeeType employeeType)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = employeeId,
            EmployeeAccount = $"employee-{employeeId}",
            EmployeeFullName = $"Employee {employeeId}",
            Status = status,
            EmployeeType = (byte)employeeType
        });

        await context.SaveChangesAsync();
    }

    private static CreateContractRequest CreateRequest(
        int? responsibleEmployeeId = null)
    {
        return new CreateContractRequest
        {
            CustomerId = CustomerId,
            ResponsibleEmployeeId = responsibleEmployeeId,
            ContractType = ContractType.SoftwareSupply,
            TemplateVersionId = TemplateVersionId,
            ContractName = "Contract kiểm thử",
            CurrencyCode = "VND",
            LanguageMode = ContractLanguageMode.Vietnamese,
            Items =
            [
                new CreateContractItemRequest
                {
                    ItemType = ContractItemType.Product,
                    ItemName = "Sản phẩm kiểm thử",
                    Quantity = 1m,
                    UnitPrice = 100m,
                    DisplayOrder = 1
                }
            ]
        };
    }
}
