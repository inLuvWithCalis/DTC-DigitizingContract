using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    private void ValidateLockedVersionChanges() =>
        ValidateLockedVersionChangesAsync(CancellationToken.None, useAsync: false)
            .GetAwaiter().GetResult();

    private Task ValidateLockedVersionChangesAsync(CancellationToken cancellationToken) =>
        ValidateLockedVersionChangesAsync(cancellationToken, useAsync: true);

    private async Task ValidateLockedVersionChangesAsync(
        CancellationToken cancellationToken,
        bool useAsync)
    {
        if (ChangeTracker.Entries<TblContractVersionLegalSnapshot>().Any(IsModifiedOrDeleted)
            || ChangeTracker.Entries<TblContractVersionPartySnapshot>().Any(IsModifiedOrDeleted)
            || ChangeTracker.Entries<TblContractVersionPaymentMilestoneSnapshot>().Any(IsModifiedOrDeleted))
        {
            throw new InvalidOperationException(
                "Relational contract snapshot là dữ liệu bất biến và không được sửa hoặc xóa.");
        }

        foreach (var entry in ChangeTracker.Entries<TblContractVersion>())
        {
            if (entry.State == EntityState.Modified
                && entry.OriginalValues.GetValue<bool>(nameof(TblContractVersion.IsLocked)))
            {
                throw new InvalidOperationException(
                    "Version đã khóa là dữ liệu bất biến và không được sửa.");
            }

            if (entry.State == EntityState.Modified
                && entry.Entity.IsLocked
                && !entry.OriginalValues.GetValue<bool>(nameof(TblContractVersion.IsLocked)))
            {
                var snapshot = ChangeTracker.Entries<TblContractVersionLegalSnapshot>()
                    .Where(candidate => candidate.State == EntityState.Added
                        && candidate.Entity.VersionId == entry.Entity.VersionId)
                    .Select(candidate => candidate.Entity)
                    .SingleOrDefault();
                if (snapshot is null
                    || snapshot.Parties.Count != 2
                    || snapshot.Parties.Select(x => x.PartyRole).Distinct().Count() != 2
                    || snapshot.Parties.Any(x => x.PartyRole is not (1 or 2))
                    || string.IsNullOrWhiteSpace(entry.Entity.SnapshotHash))
                {
                    throw new InvalidOperationException(
                        "Version chỉ được khóa cùng một relational snapshot đầy đủ hai bên.");
                }
            }
        }

        var changedVersionIds = ChangeTracker.Entries<TblContractItem>()
            .Where(IsMutated).Select(x => x.Entity.VersionId)
            .Concat(ChangeTracker.Entries<TblContractTerm>()
                .Where(IsMutated).Select(x => x.Entity.VersionId))
            .Concat(ChangeTracker.Entries<TblContractLegalBasis>()
                .Where(IsMutated).Select(x => x.Entity.VersionId))
            .Concat(ChangeTracker.Entries<TblContractVersionPlaceholderValue>()
                .Where(IsMutated).Select(x => x.Entity.VersionId))
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        if (changedVersionIds.Length == 0)
        {
            return;
        }

        var trackedLockedIds = ChangeTracker.Entries<TblContractVersion>()
            .Where(entry => changedVersionIds.Contains(entry.Entity.VersionId)
                && entry.Entity.IsLocked)
            .Select(entry => entry.Entity.VersionId)
            .ToHashSet();
        var unresolvedIds = changedVersionIds.Except(trackedLockedIds).ToArray();
        if (unresolvedIds.Length > 0)
        {
            var query = TblContractVersions.AsNoTracking()
                .Where(version => unresolvedIds.Contains(version.VersionId)
                    && version.IsLocked)
                .Select(version => version.VersionId);
            var storedLockedIds = useAsync
                ? await query.ToListAsync(cancellationToken)
                : query.ToList();
            trackedLockedIds.UnionWith(storedLockedIds);
        }

        if (trackedLockedIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Item, điều khoản, căn cứ pháp lý và placeholder của version đã khóa là dữ liệu bất biến.");
        }
    }

    private static bool IsModifiedOrDeleted<T>(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry)
        where T : class => entry.State is EntityState.Modified or EntityState.Deleted;

    private static bool IsMutated<T>(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry)
        where T : class => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
}
