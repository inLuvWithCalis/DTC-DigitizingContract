using ContractManagement.API.Domains.Models.Contract;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Persistence;

public sealed class ContractVersionImmutabilityTests
{
    [Fact]
    public async Task LockedVersion_RejectsNewContentAndSnapshotMutation()
    {
        await using var context = CreateContext();
        await SeedLockedVersionAsync(context);

        context.TblContractTerms.Add(Term(2, "LATE"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        var legalSnapshot = await context.TblContractVersionLegalSnapshots.SingleAsync();
        legalSnapshot.ContractName = "Không được sửa";
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task OperationalMilestoneChange_DoesNotRewriteLegalSnapshot()
    {
        await using var context = CreateContext();
        await SeedLockedVersionAsync(context);

        var milestone = await context.TblContractPaymentMilestones.SingleAsync();
        milestone.PaymentStatus = 2;
        milestone.PaidAt = DateTime.UtcNow;
        milestone.PaidByEmployeeId = 7;
        await context.SaveChangesAsync();

        var persistedSnapshot = await context
            .TblContractVersionPaymentMilestoneSnapshots
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal((byte)1, persistedSnapshot.PaymentStatus);
        Assert.Null(persistedSnapshot.PaidAt);
        Assert.Null(persistedSnapshot.PaidByEmployeeId);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }

    private static async Task SeedLockedVersionAsync(DbDtctechContext context)
    {
        var version = new TblContractVersion
        {
            VersionId = 1,
            ContractId = 1,
            VersionNo = 1,
            CurrencyCode = "VND",
            IsLocked = false,
            CreatedEmployeeId = 7,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1]
        };
        context.TblContractVersions.Add(version);
        context.TblContractTerms.Add(Term(1, "PAYMENT"));
        context.TblContractPaymentMilestones.Add(new TblContractPaymentMilestone
        {
            PaymentMilestoneId = 1,
            ContractId = 1,
            VersionId = 1,
            TermId = 1,
            MilestoneCode = "M1",
            TitleVi = "Đợt 1",
            PaymentPercent = 100,
            DueAnchor = 1,
            DayCountMode = 1,
            DisplayOrder = 1,
            Amount = 100,
            PaymentStatus = 1,
            CreatedEmployeeId = 7,
            CreatedDate = DateTime.UtcNow,
            RowVersion = [1]
        });
        await context.SaveChangesAsync();

        var snapshot = ContractSnapshotTestData.Create(1, 1) with
        {
            PaymentMilestones =
            [
                new ContractPaymentMilestoneSnapshot(
                    1, 1, null, "M1", "Đợt 1", null, 100, 1, 0, 1,
                    null, null, 1, 100, null, null, 1, null, null)
            ]
        };
        version.IsLocked = true;
        version.LockedDate = DateTime.UtcNow;
        version.LockedByEmployeeId = 7;
        version.SnapshotHash = SoftwareSupplyContractSnapshotFactory.CalculateHash(snapshot);
        context.TblContractVersionLegalSnapshots.Add(
            SoftwareSupplyContractSnapshotFactory.CreatePersistenceGraph(
                snapshot, 7, DateTime.UtcNow));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static TblContractTerm Term(int id, string code) => new()
    {
        TermId = id,
        ContractId = 1,
        VersionId = 1,
        TermCode = code,
        TermTitle = code,
        DisplayOrder = id,
        CreatedEmployeeId = 7,
        CreatedDate = DateTime.UtcNow,
        RowVersion = [1]
    };
}
