using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.File;

/// <summary>
/// Generic files do not have a permission of their own. Access is always
/// derived from the resource recorded in their metadata.
/// </summary>
public sealed class FileResourceAuthorizationService
    : IFileResourceAuthorizationService
{
    private const string ContractObjectType = "Contract";
    private const string ContractTemplateVersionObjectType =
        "ContractTemplateVersion";
    private const string ContractTemplatePreviewObjectType =
        "ContractTemplatePreview";
    private const string ContractTemplatePublishedPreviewPdfObjectType =
        "ContractTemplatePublishedPreviewPdf";
    private const string QuotationObjectType = "Quotation";

    private readonly DbDtctechContext _dbContext;
    private readonly IContractResourceAuthorizationService _contractAuthorization;

    public FileResourceAuthorizationService(
        DbDtctechContext dbContext,
        IContractResourceAuthorizationService contractAuthorization)
    {
        _dbContext = dbContext;
        _contractAuthorization = contractAuthorization;
    }

    public Task EnsureCanReadByObjectAsync(
        string objectType,
        int objectId,
        int employeeId,
        CancellationToken cancellationToken = default) =>
        EnsureByObjectAsync(
            objectType,
            objectId,
            employeeId,
            isWrite: false,
            cancellationToken);

    public Task EnsureCanWriteByObjectAsync(
        string objectType,
        int objectId,
        int employeeId,
        CancellationToken cancellationToken = default) =>
        EnsureByObjectAsync(
            objectType,
            objectId,
            employeeId,
            isWrite: true,
            cancellationToken);

    public async Task EnsureCanReadFileAsync(
        int fileId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var file = await GetFileAsync(fileId, cancellationToken);
        await EnsureCanReadByObjectAsync(
            file.ObjectType,
            file.ObjectId,
            employeeId,
            cancellationToken);
    }

    public async Task EnsureCanDeleteFileAsync(
        int fileId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var file = await GetFileAsync(fileId, cancellationToken);
        await EnsureCanWriteByObjectAsync(
            file.ObjectType,
            file.ObjectId,
            employeeId,
            cancellationToken);
    }

    private async Task EnsureByObjectAsync(
        string objectType,
        int objectId,
        int employeeId,
        bool isWrite,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectType) || objectId <= 0)
        {
            throw ResourceNotFound();
        }

        switch (objectType)
        {
            case ContractObjectType:
                if (isWrite)
                {
                    await _contractAuthorization.EnsureCanWriteAsync(
                        objectId, employeeId, cancellationToken);
                }
                else
                {
                    await _contractAuthorization.EnsureCanReadAsync(
                        objectId, employeeId, cancellationToken);
                }

                return;

            case ContractTemplateVersionObjectType:
            case ContractTemplatePreviewObjectType:
            case ContractTemplatePublishedPreviewPdfObjectType:
                await EnsureAdminOfficerAsync(employeeId, cancellationToken);
                if (!await _dbContext.TblContractTemplateVersions
                        .AsNoTracking()
                        .AnyAsync(version => version.TemplateVersionId == objectId,
                            cancellationToken))
                {
                    throw ResourceNotFound();
                }

                return;

            case QuotationObjectType:
                await EnsureActiveEmployeeAsync(employeeId, cancellationToken);
                if (!await _dbContext.TblQuotations
                        .AsNoTracking()
                        .AnyAsync(quotation => quotation.QuotationId == objectId,
                            cancellationToken))
                {
                    throw ResourceNotFound();
                }

                return;

            default:
                throw new RbacOperationException(
                    StatusCodes.Status403Forbidden,
                    AuthorizationErrorCodes.PermissionDenied,
                    "This file object type is not supported.");
        }
    }

    private async Task<(string ObjectType, int ObjectId)> GetFileAsync(
        int fileId,
        CancellationToken cancellationToken)
    {
        var file = await _dbContext.TblFileStorages
            .AsNoTracking()
            .Where(candidate => candidate.FileId == fileId)
            .Select(candidate => new
            {
                candidate.ObjectType,
                candidate.ObjectId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return file is null
            ? throw ResourceNotFound()
            : (file.ObjectType, file.ObjectId);
    }

    private async Task EnsureActiveEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.TblEmployees
                .AsNoTracking()
                .AnyAsync(employee => employee.EmployeeId == employeeId
                    && employee.Status == 1,
                    cancellationToken))
        {
            throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.EmployeeInactive,
                "Employee account is inactive.");
        }
    }

    private async Task EnsureAdminOfficerAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var allowed = await _dbContext.TblEmployees
            .AsNoTracking()
            .AnyAsync(employee => employee.EmployeeId == employeeId
                && employee.Status == 1
                && employee.EmployeeType == (byte)EmployeeType.AdminOfficer,
                cancellationToken);

        if (!allowed)
        {
            throw new RbacOperationException(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Only Admin Officer may access template files.");
        }
    }

    private static RbacOperationException ResourceNotFound() => new(
        StatusCodes.Status404NotFound,
        AuthorizationErrorCodes.ResourceNotFound,
        "Resource was not found.");
}
