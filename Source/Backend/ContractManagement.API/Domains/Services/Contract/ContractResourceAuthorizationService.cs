using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Contract scope is enforced in the tenant database. Managers can read every
/// tenant contract, while every write still belongs to its responsible employee.
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
        var manager = await IsManagerAsync(employeeId, cancellationToken);
        var authorized = await _dbContext.TblContracts
            .AsNoTracking()
            .AnyAsync(contract =>
                contract.ContractId == contractId
                && (manager || contract.EmployeeId == employeeId),
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

    private async Task<bool> IsManagerAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TblEmployees
            .AsNoTracking()
            .AnyAsync(employee =>
                employee.EmployeeId == employeeId
                && employee.Status == 1
                && employee.EmployeeType == (byte)EmployeeType.Manager,
                cancellationToken);
    }

    private static RbacOperationException ResourceNotFound() => new(
        StatusCodes.Status404NotFound,
        AuthorizationErrorCodes.ResourceNotFound,
        "Resource was not found.");
}
