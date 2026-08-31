using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Domains.Services.File;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Domains.Security;

public sealed class Slice04ResourceAuthorizationTests
{
    private const int ResponsibleEmployeeId = 1;
    private const int ManagerEmployeeId = 2;
    private const int OtherEmployeeId = 3;
    private const int AdminOfficerEmployeeId = 4;
    private const int ContractId = 100;

    [Fact]
    public async Task Manager_ReadsTenantContract_ButCannotWriteAnotherEmployeesContract()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new ContractResourceAuthorizationService(context);

        await service.EnsureCanReadAsync(ContractId, ManagerEmployeeId);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanWriteAsync(ContractId, ManagerEmployeeId));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.ResourceNotFound, exception.Code);
    }

    [Fact]
    public async Task UnrelatedEmployee_CannotDiscoverAnotherEmployeesContract()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new ContractResourceAuthorizationService(context);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanReadAsync(ContractId, OtherEmployeeId));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.ResourceNotFound, exception.Code);
    }

    [Fact]
    public async Task ContractInAnotherTenantDatabase_IsNotDiscoverable()
    {
        await using var tenantA = CreateContext();
        await SeedAsync(tenantA);
        await using var tenantB = CreateContext();
        tenantB.TblEmployees.Add(Employee(OtherEmployeeId, EmployeeType.Sale));
        await tenantB.SaveChangesAsync();
        var service = new ContractResourceAuthorizationService(tenantB);

        var exception = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanReadAsync(ContractId, OtherEmployeeId));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.ResourceNotFound, exception.Code);
    }

    [Fact]
    public async Task ContractList_UsesManagerTenantScopeOnlyWhenExplicitlyAuthorized()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        context.TblContracts.Add(new TblContract
        {
            ContractId = 101,
            CustomerId = 10,
            EmployeeId = OtherEmployeeId,
            ContractName = "Other contract",
            ContractType = (byte)ContractType.SoftwareSupply,
            Status = (byte)ContractStatus.Draft,
            CurrencyCode = "VND",
            CreatedEmployeeId = OtherEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 1]
        });
        await context.SaveChangesAsync();

        var service = new ContractService(context, new NoOpContractAuditWriter());
        var ownScope = await service.GetListAsync(
            new ContractFilterRequest { Page = 1, PageSize = 20 },
            ManagerEmployeeId);
        var tenantScope = await service.GetListAsync(
            new ContractFilterRequest { Page = 1, PageSize = 20 },
            ManagerEmployeeId,
            canReadTenant: true);

        Assert.Empty(ownScope.Items);
        Assert.Equal(new[] { ContractId, 101 },
            tenantScope.Items.Select(item => item.ContractId).Order());
    }

    [Fact]
    public async Task GenericFileAccess_FollowsOwningResourcePolicy()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        context.TblFileStorages.AddRange(
            new TblFileStorage
            {
                FileId = 1,
                ObjectType = "Contract",
                ObjectId = ContractId,
                FileName = "contract.pdf",
                FilePath = "/uploads/tenant/Contract/100/contract.pdf",
                UploadedDate = DateTime.UtcNow
            },
            new TblFileStorage
            {
                FileId = 2,
                ObjectType = "ContractTemplateVersion",
                ObjectId = 500,
                FileName = "template.docx",
                FilePath = "/uploads/tenant/ContractTemplateVersion/500/template.docx",
                UploadedDate = DateTime.UtcNow
            },
            new TblFileStorage
            {
                FileId = 3,
                ObjectType = "ContractVersionArtifact",
                ObjectId = 600,
                FileName = "submitted.pdf",
                FilePath = string.Empty,
                StorageKey = "tenant/ContractVersionArtifact/600/submitted.pdf",
                TenantCode = "tenant",
                UploadedDate = DateTime.UtcNow
            });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = 600,
            ContractId = ContractId,
            VersionNo = 1,
            CurrencyCode = "VND",
            SnapshotJson = "{\"schemaVersion\":4}",
            SnapshotHash = new string('a', 64),
            IsLocked = true,
            CreatedEmployeeId = ResponsibleEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [6, 0, 0, 0, 0, 0, 0, 0]
        });
        context.TblContractTemplateVersions.Add(new TblContractTemplateVersion
        {
            TemplateVersionId = 500,
            TemplateId = 50,
            VersionNo = 1,
            Status = 2,
            ValidationStatus = 2,
            CreatedEmployeeId = AdminOfficerEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [5, 0, 0, 0, 0, 0, 0, 0]
        });
        await context.SaveChangesAsync();

        var contractAuthorization = new ContractResourceAuthorizationService(context);
        var service = new FileResourceAuthorizationService(
            context,
            contractAuthorization);

        await service.EnsureCanReadFileAsync(1, ManagerEmployeeId);
        await service.EnsureCanDeleteFileAsync(1, ResponsibleEmployeeId);
        await service.EnsureCanReadFileAsync(2, AdminOfficerEmployeeId);
        await service.EnsureCanReadFileAsync(3, ResponsibleEmployeeId);
        await service.EnsureCanReadFileAsync(3, ManagerEmployeeId);

        var deniedContractRead = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanReadFileAsync(1, OtherEmployeeId));
        var deniedTemplateRead = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanReadFileAsync(2, ManagerEmployeeId));
        var deniedArtifactRead = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanReadFileAsync(3, OtherEmployeeId));
        var deniedArtifactDelete = await Assert.ThrowsAsync<RbacOperationException>(
            () => service.EnsureCanDeleteFileAsync(3, ResponsibleEmployeeId));

        Assert.Equal(AuthorizationErrorCodes.ResourceNotFound, deniedContractRead.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, deniedTemplateRead.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, deniedTemplateRead.Code);
        Assert.Equal(StatusCodes.Status404NotFound, deniedArtifactRead.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.ResourceNotFound, deniedArtifactRead.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, deniedArtifactDelete.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, deniedArtifactDelete.Code);
    }

    [Fact]
    public async Task GenericFileAccess_RejectsUnknownObjectPolicy()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new FileResourceAuthorizationService(
            context,
            new ContractResourceAuthorizationService(context));

        var exception = await Assert.ThrowsAsync<RbacOperationException>(() =>
            service.EnsureCanReadByObjectAsync(
                "UnapprovedObject",
                1,
                ResponsibleEmployeeId));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal(AuthorizationErrorCodes.PermissionDenied, exception.Code);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedAsync(DbDtctechContext context)
    {
        context.TblEmployees.AddRange(
            Employee(ResponsibleEmployeeId, EmployeeType.Sale),
            Employee(ManagerEmployeeId, EmployeeType.Manager),
            Employee(OtherEmployeeId, EmployeeType.Technical),
            Employee(AdminOfficerEmployeeId, EmployeeType.AdminOfficer));
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = 10,
            CustomerFullName = "Customer",
            Status = 1
        });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = 10,
            EmployeeId = ResponsibleEmployeeId,
            ContractName = "Responsible contract",
            ContractType = (byte)ContractType.SoftwareSupply,
            Status = (byte)ContractStatus.Draft,
            CurrencyCode = "VND",
            CreatedEmployeeId = ResponsibleEmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 0]
        });
        await context.SaveChangesAsync();
    }

    private static TblEmployee Employee(int id, EmployeeType type) => new()
    {
        EmployeeId = id,
        EmployeeAccount = $"employee-{id}",
        EmployeeFullName = $"Employee {id}",
        EmployeeType = (byte)type,
        Status = 1
    };

    private sealed class NoOpContractAuditWriter : IContractAuditWriter
    {
        public void StageAudits(IReadOnlyCollection<ContractAuditWriteRequest> requests)
        {
        }

        public void StageEmployeeAudits(
            IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests)
        {
        }
    }
}
