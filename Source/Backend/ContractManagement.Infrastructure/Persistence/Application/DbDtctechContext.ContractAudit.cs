using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateContractAuditEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(
            acceptAllChangesOnSuccess: true,
            cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateContractAuditEntries();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void ValidateContractAuditEntries()
    {
        var changedEntries = ChangeTracker
            .Entries<TblContractAudit>()
            .Where(entry =>
                entry.State == EntityState.Modified
                || entry.State == EntityState.Deleted)
            .ToList();

        if (changedEntries.Count > 0)
        {
            throw new InvalidOperationException(
                "Contract audit là dữ liệu append-only và không được sửa hoặc xóa.");
        }

        foreach (var entry in ChangeTracker
                     .Entries<TblContractAudit>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            ValidateNewContractAudit(entry.Entity);
        }
    }

    private static void ValidateNewContractAudit(TblContractAudit audit)
    {
        if (audit.TenantId <= 0
            || audit.ContractId <= 0
            || audit.VersionId is <= 0)
        {
            throw new InvalidOperationException(
                "Contract audit phải tham chiếu Tenant, Contract và Version hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(audit.ActorType))
        {
            throw new InvalidOperationException(
                "Contract audit phải có ActorType.");
        }

        var isEmployeeActor = string.Equals(
            audit.ActorType,
            "Employee",
            StringComparison.Ordinal);

        if (isEmployeeActor && audit.ActorEmployeeId is not > 0)
        {
            throw new InvalidOperationException(
                "Employee audit actor phải có ActorEmployeeId hợp lệ.");
        }

        if (!isEmployeeActor && audit.ActorEmployeeId.HasValue)
        {
            throw new InvalidOperationException(
                "Non-employee audit actor không được có ActorEmployeeId.");
        }

        if (audit.PreviousResponsibleEmployeeId is <= 0
            || audit.NewResponsibleEmployeeId is <= 0)
        {
            throw new InvalidOperationException(
                "Responsible employee trong Contract audit phải hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(audit.ActionType)
            || string.IsNullOrWhiteSpace(audit.Result)
            || string.IsNullOrWhiteSpace(audit.CorrelationId))
        {
            throw new InvalidOperationException(
                "Contract audit phải có action, result và correlation.");
        }

        if (audit.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "Contract audit phải sử dụng timestamp UTC.");
        }
    }
}
