using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    private static long _syntheticRowVersionSeed = 10_000;

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RevokeCustomerAccessForCancelledContracts();
        AssignSyntheticRowVersionsForInMemory();
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
        RevokeCustomerAccessForCancelledContracts();
        AssignSyntheticRowVersionsForInMemory();
        ValidateContractAuditEntries();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void AssignSyntheticRowVersionsForInMemory()
    {
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries()
                     .Where(x => x.State == EntityState.Added
                         && x.Metadata.FindProperty("RowVersion") is not null))
        {
            var property = entry.Property("RowVersion");
            if (property.CurrentValue is not byte[] { Length: 8 })
            {
                property.CurrentValue = BitConverter.GetBytes(
                    Interlocked.Increment(ref _syntheticRowVersionSeed));
            }
        }
    }

    private void RevokeCustomerAccessForCancelledContracts()
    {
        var cancelledContracts = ChangeTracker.Entries<TblContract>()
            .Where(entry => entry.State == EntityState.Modified
                && entry.Entity.Status == 6
                && entry.OriginalValues.GetValue<byte>(nameof(TblContract.Status)) != 6)
            .Select(entry => entry.Entity)
            .ToList();

        if (cancelledContracts.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var contract in cancelledContracts)
        {
            var actorEmployeeId = contract.UpdatedEmployeeId ?? contract.EmployeeId;
            var links = TblContractCustomerAccessLinks
                .Where(link => link.ContractId == contract.ContractId
                    && link.RevokedAt == null)
                .ToList();
            foreach (var link in links)
            {
                link.RevokedAt = now;
                link.RevokedByEmployeeId = actorEmployeeId;
                link.RevocationReason = "Contract cancelled";

                foreach (var challenge in TblContractCustomerOtpChallenges
                             .Where(challenge => challenge.LinkId
                                 == link.CustomerAccessLinkId
                                 && challenge.InvalidatedAt == null)
                             .ToList())
                {
                    challenge.InvalidatedAt = now;
                }

                foreach (var session in TblContractCustomerAccessSessions
                             .Where(session => session.LinkId
                                 == link.CustomerAccessLinkId
                                 && session.RevokedAt == null)
                             .ToList())
                {
                    session.RevokedAt = now;
                    session.RevocationReason = "Contract cancelled";
                }
            }

            contract.CurrentCustomerAccessLinkId = null;
        }
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

        var isCustomerActor = string.Equals(
            audit.ActorType,
            "Customer",
            StringComparison.Ordinal);
        var isSystemActor = string.Equals(
            audit.ActorType,
            "System",
            StringComparison.Ordinal);

        if (!isEmployeeActor && !isCustomerActor && !isSystemActor)
        {
            throw new InvalidOperationException(
                "Contract audit actor type is invalid.");
        }

        if ((isEmployeeActor && audit.ActorCustomerAccessSessionId.HasValue)
            || (isCustomerActor
                && audit.ActorCustomerAccessSessionId is not > 0)
            || (isSystemActor
                && audit.ActorCustomerAccessSessionId.HasValue))
        {
            throw new InvalidOperationException(
                "Contract audit actor is inconsistent.");
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
