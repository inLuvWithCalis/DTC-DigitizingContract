using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Contract scope is enforced in the tenant database. Content writes still
/// belong to the responsible employee, while execution-stage mutations use
/// explicit signing, acceptance, payment, and completion permissions.
/// </summary>
public sealed class ContractResourceAuthorizationService
    : IContractResourceAuthorizationService
{
    private readonly DbDtctechContext _dbContext;

    public ContractResourceAuthorizationService(DbDtctechContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureCanReadAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetActiveEmployeePermissionsAsync(
            employeeId,
            cancellationToken);
        var canReadTenant = permissions.Contains(
            RbacPermissions.ContractReadTenant,
            StringComparer.Ordinal);
        var canReadExecution = permissions.Contains(
            RbacPermissions.ContractExecutionRead,
            StringComparer.Ordinal);
        var authorized = await _dbContext.TblContracts
            .AsNoTracking()
            .AnyAsync(contract =>
                contract.ContractId == contractId
                && (canReadTenant
                    || contract.EmployeeId == employeeId
                    || (canReadExecution
                        && (contract.Status ==
                                (byte)ContractStatus.PendingSignature
                            || contract.Status ==
                                (byte)ContractStatus.Signed
                            || contract.Status ==
                                (byte)ContractStatus.Completed))),
                cancellationToken);

        if (!authorized)
        {
            throw ResourceNotFound();
        }
    }

    public async Task EnsureCanWriteAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await _dbContext.TblContracts
            .AsNoTracking()
            .AnyAsync(contract =>
                contract.ContractId == contractId
                && contract.EmployeeId == employeeId,
                cancellationToken);

        if (!authorized)
        {
            throw ResourceNotFound();
        }
    }

    public Task EnsureCanManageSigningAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default) =>
        EnsureHasExecutionPermissionAsync(
            contractId,
            employeeId,
            RbacPermissions.ContractSigningManage,
            cancellationToken);

    public Task EnsureCanManageAcceptanceAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default) =>
        EnsureHasExecutionPermissionAsync(
            contractId,
            employeeId,
            RbacPermissions.ContractAcceptanceManage,
            cancellationToken);

    public Task EnsureCanManagePaymentAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default) =>
        EnsureHasExecutionPermissionAsync(
            contractId,
            employeeId,
            RbacPermissions.ContractPaymentManage,
            cancellationToken);

    public Task EnsureCanCompleteAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default) =>
        EnsureHasExecutionPermissionAsync(
            contractId,
            employeeId,
            RbacPermissions.ContractComplete,
            cancellationToken);

    private async Task EnsureHasExecutionPermissionAsync(
        int contractId,
        int employeeId,
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        var permissions = await GetActiveEmployeePermissionsAsync(
            employeeId,
            cancellationToken);
        if (!permissions.Contains(requiredPermission, StringComparer.Ordinal))
        {
            throw PermissionDenied();
        }

        var contractExists = await _dbContext.TblContracts
            .AsNoTracking()
            .AnyAsync(contract => contract.ContractId == contractId,
                cancellationToken);

        if (!contractExists)
        {
            throw ResourceNotFound();
        }
    }

    private async Task<IReadOnlyList<string>> GetActiveEmployeePermissionsAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employeeType = await _dbContext.TblEmployees
            .AsNoTracking()
            .Where(employee =>
                employee.EmployeeId == employeeId
                && employee.Status == 1)
            .Select(employee => employee.EmployeeType)
            .SingleOrDefaultAsync(cancellationToken);

        if (!EmployeePermissionCatalog.TryGetPermissions(
                employeeType,
                out var permissions))
        {
            return Array.Empty<string>();
        }

        return permissions;
    }

    private static RbacOperationException ResourceNotFound() => new(
        StatusCodes.Status404NotFound,
        AuthorizationErrorCodes.ResourceNotFound,
        "Resource was not found.");

    private static RbacOperationException PermissionDenied() => new(
        StatusCodes.Status403Forbidden,
        AuthorizationErrorCodes.PermissionDenied,
        "Employee does not have permission to perform this contract operation.");
}
