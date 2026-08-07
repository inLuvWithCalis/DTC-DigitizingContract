using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
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

public sealed class ContractServiceSlice05Tests
{
    private const int TenantId = 905;
    private const int ContractId = 500;
    private const int VersionId = 501;
    private const int CustomerId = 502;
    private const int EmployeeId = 503;
    private const int OtherEmployeeId = 504;
    private const int NegotiableTermId = 505;
    private const int NonNegotiableTermId = 506;

    [Fact]
    public async Task ExternalFeedback_TermReplyResolveAndReopen_ShouldBeAppendOnly()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var service = CreateService(context);

        var root = await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                Content = "  General feedback  "
            },
            EmployeeId);

        var termComment = await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                TermId = NegotiableTermId,
                Content = "Term feedback"
            },
            EmployeeId);

        var reply = await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                ParentCommentId = termComment.CommentId,
                Content = "  Reply inherits the term  "
            },
            EmployeeId);

        Assert.Equal("General feedback", root.Content);
        Assert.Null(root.TermId);
        Assert.Equal(NegotiableTermId, termComment.TermId);
        Assert.Equal(termComment.TermId, reply.TermId);
        Assert.Single(root.Events);
        Assert.Equal(
            ContractNegotiationCommentEventType.Created,
            root.Events[0].EventType);

        var beforeResolveRowVersion = reply.RowVersion;
        var resolved = await service.ResolveCommentAsync(
            ContractId,
            reply.CommentId,
            new UpdateContractNegotiationCommentStateRequest
            {
                RowVersion = beforeResolveRowVersion
            },
            EmployeeId);

        Assert.Equal(
            ContractNegotiationCommentState.Resolved,
            resolved.State);
        Assert.Equal(2, resolved.Events.Count);

        var reopened = await service.ReopenCommentAsync(
            ContractId,
            reply.CommentId,
            new UpdateContractNegotiationCommentStateRequest
            {
                RowVersion = resolved.RowVersion
            },
            EmployeeId);

        Assert.Equal(ContractNegotiationCommentState.Open, reopened.State);
        Assert.Equal(3, reopened.Events.Count);
        Assert.Equal(
            "Reply inherits the term",
            (await context.TblContractNegotiationComments
                .AsNoTracking()
                .SingleAsync(x => x.CommentId == reply.CommentId)).Content);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => service.ResolveCommentAsync(
                ContractId,
                reply.CommentId,
                new UpdateContractNegotiationCommentStateRequest
                {
                    RowVersion = beforeResolveRowVersion
                },
                EmployeeId));

        var events = await context
            .TblContractNegotiationCommentEvents
            .AsNoTracking()
            .Where(x => x.CommentId == reply.CommentId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();

        Assert.Equal(3, events.Count);
        Assert.Contains(
            context.TblContractAudits,
            audit => audit.ActionType ==
                ContractAuditActionTypes.ExternalFeedbackCreated
                && audit.Result == ContractAuditResults.Succeeded);
        Assert.Contains(
            context.TblContractAudits,
            audit => audit.ActionType ==
                ContractAuditActionTypes.NegotiationReplyCreated
                && audit.Result == ContractAuditResults.Succeeded);
        Assert.Contains(
            context.TblContractAudits,
            audit => audit.ActionType ==
                ContractAuditActionTypes.NegotiationCommentResolved
                && audit.Result == ContractAuditResults.Succeeded);
        Assert.Contains(
            context.TblContractAudits,
            audit => audit.ActionType ==
                ContractAuditActionTypes.NegotiationCommentReopened
                && audit.Result == ContractAuditResults.Succeeded);
        Assert.Contains(
            context.TblContractAudits,
            audit => audit.ActionType ==
                ContractAuditActionTypes.NegotiationCommentResolved
                && audit.Result == ContractAuditResults.ConcurrencyConflict
                && audit.FailureCode == ContractAuditFailureCodes.StaleRowVersion);
    }

    [Fact]
    public async Task CommentQueries_ShouldReturnRootsAndDirectRepliesOnly()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var service = CreateService(context);

        var firstRoot = await service.CreateExternalFeedbackAsync(
            ContractId,
            NewCommentRequest("First root"),
            EmployeeId);

        var secondRoot = await service.CreateExternalFeedbackAsync(
            ContractId,
            NewCommentRequest("Second root"),
            EmployeeId);

        var directReply = await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                ParentCommentId = firstRoot.CommentId,
                Content = "Direct reply"
            },
            EmployeeId);

        await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                ParentCommentId = directReply.CommentId,
                Content = "Nested reply"
            },
            EmployeeId);

        var roots = await service.GetRootCommentsAsync(
            ContractId,
            EmployeeId);
        var replies = await service.GetCommentRepliesAsync(
            ContractId,
            firstRoot.CommentId,
            EmployeeId);

        Assert.Equal(
            [firstRoot.CommentId, secondRoot.CommentId],
            roots.Select(x => x.CommentId));
        Assert.Single(replies);
        Assert.Equal(directReply.CommentId, replies[0].CommentId);
        Assert.Equal(firstRoot.CommentId, replies[0].ParentCommentId);
        Assert.Single(replies[0].Events);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetRootCommentsAsync(
                ContractId,
                OtherEmployeeId));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetCommentRepliesAsync(
                ContractId,
                directReply.CommentId,
                EmployeeId));
    }

    [Fact]
    public async Task ExternalFeedback_ShouldRejectNonNegotiableAndCrossVersionTerm()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    TermId = NonNegotiableTermId,
                    Content = "Not allowed"
                },
                EmployeeId));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    TermId = 9999,
                    Content = "Wrong version target"
                },
                EmployeeId));

        Assert.Empty(context.TblContractNegotiationComments);
    }

    [Fact]
    public async Task Reply_ShouldRejectResolvedParentAndDifferentTerm()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var service = CreateService(context);

        var parent = await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                TermId = NegotiableTermId,
                Content = "Parent"
            },
            EmployeeId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    ParentCommentId = parent.CommentId,
                    TermId = NonNegotiableTermId,
                    Content = "Different target"
                },
                EmployeeId));

        parent = await service.ResolveCommentAsync(
            ContractId,
            parent.CommentId,
            new UpdateContractNegotiationCommentStateRequest
            {
                RowVersion = parent.RowVersion
            },
            EmployeeId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    ParentCommentId = parent.CommentId,
                    Content = "Resolved parent"
                },
                EmployeeId));
    }

    [Fact]
    public async Task ResponsibilityTransfer_ShouldRejectOldEmployeeLifecycleActions()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var service = CreateService(context);

        var comment = await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                Content = "Before transfer"
            },
            EmployeeId);

        var contract = await context.TblContracts.SingleAsync();
        contract.EmployeeId = OtherEmployeeId;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    Content = "Old responsible"
                },
                EmployeeId));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => service.ResolveCommentAsync(
                ContractId,
                comment.CommentId,
                new UpdateContractNegotiationCommentStateRequest
                {
                    RowVersion = comment.RowVersion
                },
                EmployeeId));

        Assert.Equal(
            ContractNegotiationCommentState.Open,
            (ContractNegotiationCommentState)(await context
                .TblContractNegotiationComments
                .SingleAsync()).State);
    }

    [Fact]
    public async Task LockedOrNonNegotiatingContract_ShouldRejectWrites()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context);
        var service = CreateService(context);

        var version = await context.TblContractVersions.SingleAsync();
        version.IsLocked = true;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    Content = "Locked"
                },
                EmployeeId));

        version.IsLocked = false;
        var contract = await context.TblContracts.SingleAsync();
        contract.Status = (byte)ContractStatus.Draft;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                new CreateContractNegotiationCommentRequest
                {
                    CurrentVersionId = VersionId,
                    Content = "Draft"
                },
                EmployeeId));
    }

    [Fact]
    public async Task VersionHistory_ShouldKeepSourceCommentsAndNotCopyThemToNewRound()
    {
        await using var context = CreateContext();
        await SeedContractAsync(context, includeItem: true);
        var service = CreateService(context);

        await service.CreateExternalFeedbackAsync(
            ContractId,
            new CreateContractNegotiationCommentRequest
            {
                CurrentVersionId = VersionId,
                Content = "Source comment"
            },
            EmployeeId);

        var round = await service.CreateNegotiationRoundAsync(
            ContractId,
            new CreateContractNegotiationRoundRequest
            {
                CurrentVersionId = VersionId,
                RowVersion = RowVersionString(),
                CurrentVersionRowVersion = RowVersionString(),
                ChangeNote = "Round 2"
            },
            EmployeeId);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => service.CreateExternalFeedbackAsync(
                ContractId,
                NewCommentRequest("Source is no longer current"),
                EmployeeId));

        var history = await service.GetVersionHistoryAsync(
            ContractId,
            EmployeeId);
        var source = await service.GetVersionDetailAsync(
            ContractId,
            VersionId,
            EmployeeId);
        var current = await service.GetVersionDetailAsync(
            ContractId,
            round.CurrentVersion.VersionId,
            EmployeeId);

        Assert.Equal(2, history.Count);
        Assert.Single(source.Comments);
        Assert.Empty(current.Comments);
        Assert.True(source.IsLocked);
        Assert.False(current.IsLocked);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetVersionHistoryAsync(
                ContractId,
                OtherEmployeeId));
    }

    [Fact]
    public async Task TwoValidCreates_ShouldBothSucceed()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var seedContext = CreateContext(databaseName);
        await SeedContractAsync(seedContext);

        await using var firstContext = CreateContext(databaseName);
        await using var secondContext = CreateContext(databaseName);

        var first = CreateService(firstContext).CreateExternalFeedbackAsync(
            ContractId,
            NewCommentRequest("First"),
            EmployeeId);
        var second = CreateService(secondContext).CreateExternalFeedbackAsync(
            ContractId,
            NewCommentRequest("Second"),
            EmployeeId);

        await Task.WhenAll(first, second);

        Assert.Equal(
            2,
            await seedContext.TblContractNegotiationComments.CountAsync());
    }

    private static CreateContractNegotiationCommentRequest NewCommentRequest(
        string content)
    {
        return new CreateContractNegotiationCommentRequest
        {
            CurrentVersionId = VersionId,
            Content = content
        };
    }

    private static DbDtctechContext CreateContext(
        string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(
                databaseName ?? Guid.NewGuid().ToString(),
                databaseOptions => databaseOptions.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(
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
            "TENANT-905",
            "Tenant 905",
            TenantDatabaseMode.Dedicated,
            "InMemory"));

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "slice-05-test"
            }
        };

        return new ContractService(
            context,
            new ContractAuditWriter(context, tenant, accessor));
    }

    private static async Task SeedContractAsync(
        DbDtctechContext context,
        bool includeItem = false)
    {
        var rowVersion = InitialRowVersion();

        context.TblEmployees.AddRange(
            new TblEmployee
            {
                EmployeeId = EmployeeId,
                EmployeeAccount = "slice05-owner",
                EmployeeFullName = "Slice 05 owner",
                EmployeeType = (byte)EmployeeType.Sale,
                Status = 1
            },
            new TblEmployee
            {
                EmployeeId = OtherEmployeeId,
                EmployeeAccount = "slice05-other",
                EmployeeFullName = "Slice 05 other",
                EmployeeType = (byte)EmployeeType.Technical,
                Status = 1
            });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Slice 05 customer",
            Status = 1
        });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = CustomerId,
            EmployeeId = EmployeeId,
            ContractType = 1,
            CurrentVersionId = VersionId,
            ContractCode = "HD-S05",
            ContractName = "Slice 05",
            Status = (byte)ContractStatus.Negotiating,
            CurrencyCode = "VND",
            LanguageMode = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = VersionId,
            ContractId = ContractId,
            VersionNo = 1,
            CurrencyCode = "VND",
            IsLocked = false,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = rowVersion
        });
        context.TblContractTerms.AddRange(
            new TblContractTerm
            {
                TermId = NegotiableTermId,
                ContractId = ContractId,
                VersionId = VersionId,
                TermCode = "NEGOTIABLE",
                TermTitle = "Negotiable",
                IsNegotiable = true,
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = rowVersion
            },
            new TblContractTerm
            {
                TermId = NonNegotiableTermId,
                ContractId = ContractId,
                VersionId = VersionId,
                TermCode = "FIXED",
                TermTitle = "Fixed",
                IsNegotiable = false,
                DisplayOrder = 2,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = rowVersion
            });

        if (includeItem)
        {
            context.TblContractItems.Add(new TblContractItem
            {
                ContractItemId = 507,
                ContractId = ContractId,
                VersionId = VersionId,
                ItemType = (byte)ContractItemType.Product,
                ItemName = "Snapshot item",
                Quantity = 1m,
                UnitPrice = 100m,
                LineSubtotal = 100m,
                DiscountMode = 0,
                DiscountPercent = 0m,
                FixedDiscountAmount = 0m,
                DiscountAmount = 0m,
                IsTaxable = false,
                VatPercent = 0m,
                VatAmount = 0m,
                LineTotal = 100m,
                DisplayOrder = 1,
                CreatedEmployeeId = EmployeeId,
                CreatedDate = DateTime.UtcNow,
                RowVersion = rowVersion
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static string RowVersionString() =>
        Convert.ToBase64String(InitialRowVersion());

    private static byte[] InitialRowVersion() =>
        [1, 2, 3, 4, 5, 6, 7, 8];
}
