using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Dashboard;
using ContractManagement.API.Domains.DTOs.Responses.Dashboard;
using ContractManagement.API.Domains.Interfaces.Dashboard;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private const int RecentLimit = 8;
    private const int ExpiringLimit = 8;

    private readonly DbDtctechContext _dbContext;

    public DashboardService(DbDtctechContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardResponse> GetAsync(
        int employeeId,
        DashboardFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var now = DateTime.UtcNow;
        var range = NormalizeRange(filter, now);
        var employee = await _dbContext.TblEmployees
            .AsNoTracking()
            .Where(candidate => candidate.EmployeeId == employeeId
                && candidate.Status == 1)
            .Select(candidate => new
            {
                candidate.EmployeeId,
                candidate.EmployeeType
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RbacOperationException(
                StatusCodes.Status401Unauthorized,
                AuthorizationErrorCodes.EmployeeInactive,
                "Employee account is inactive or no longer exists.");

        var isManager = employee.EmployeeType == (byte)EmployeeType.Manager;
        var scopedContracts = _dbContext.TblContracts
            .AsNoTracking()
            .Where(contract => isManager || contract.EmployeeId == employeeId);
        var periodContracts = scopedContracts.Where(contract =>
            contract.CreatedDate >= range.FromUtc
            && contract.CreatedDate <= range.ToUtc);
        var previousContracts = scopedContracts.Where(contract =>
            contract.CreatedDate >= range.PreviousFromUtc
            && contract.CreatedDate < range.FromUtc);

        var currentRows = await periodContracts
            .Select(contract => new ContractMetricRow(
                contract.Status,
                contract.TotalAmount,
                contract.CurrencyCode,
                contract.CreatedDate))
            .ToListAsync(cancellationToken);
        var previousStatuses = await previousContracts
            .Select(contract => contract.Status)
            .ToListAsync(cancellationToken);

        var expiryTo = now.AddDays(filter.ExpiryDays);
        var expiringQuery = scopedContracts.Where(contract =>
            contract.ExpireDate.HasValue
            && contract.ExpireDate.Value >= now
            && contract.ExpireDate.Value <= expiryTo
            && (contract.Status == (byte)ContractStatus.Signed
                || contract.Status == (byte)ContractStatus.Completed));
        var expiringCount = await expiringQuery.CountAsync(cancellationToken);
        var expiringContracts = await (
            from contract in expiringQuery
            join employeeRow in _dbContext.TblEmployees.AsNoTracking()
                on contract.EmployeeId equals employeeRow.EmployeeId into employees
            from responsible in employees.DefaultIfEmpty()
            orderby contract.ExpireDate, contract.ContractId
            select new ExpiringContractResponse(
                contract.ContractId,
                contract.ContractCode ?? $"#{contract.ContractId}",
                contract.ContractName,
                contract.ExpireDate!.Value,
                responsible == null ? null : responsible.EmployeeFullName))
            .Take(ExpiringLimit)
            .ToListAsync(cancellationToken);

        var scopedContractIds = scopedContracts.Select(contract => contract.ContractId);
        var recentAuditRows = await _dbContext.TblContractAudits
            .AsNoTracking()
            .Where(audit => scopedContractIds.Contains(audit.ContractId)
                && audit.OccurredAt >= range.FromUtc
                && audit.OccurredAt <= range.ToUtc)
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.ContractAuditId)
            .Take(RecentLimit)
            .Select(audit => new
            {
                audit.ContractAuditId,
                audit.ContractId,
                audit.ContractCodeSnapshot,
                audit.ActionType,
                audit.ActorDisplayNameSnapshot,
                audit.ActorEmployeeId,
                audit.ActorCustomerAccessSessionId,
                audit.ActorType,
                audit.OccurredAt
            })
            .ToListAsync(cancellationToken);

        return new DashboardResponse
        {
            Scope = isManager ? "Tenant" : "Own",
            GeneratedAt = now,
            FromUtc = range.FromUtc,
            ToUtc = range.ToUtc,
            Summary = BuildSummary(
                currentRows.Select(row => row.Status),
                previousStatuses,
                expiringCount),
            AmountByCurrency = currentRows
                .GroupBy(row => NormalizeCurrency(row.CurrencyCode))
                .OrderBy(group => group.Key)
                .Select(group => new DashboardCurrencyAmountResponse(
                    group.Key,
                    group.Sum(row => row.TotalAmount)))
                .ToList(),
            VolumeSeries = BuildVolumeSeries(currentRows, range),
            StatusDistribution = currentRows
                .GroupBy(row => row.Status)
                .OrderBy(group => group.Key)
                .Select(group => new DashboardStatusPointResponse(
                    GetStatusName(group.Key),
                    group.Count()))
                .ToList(),
            ExpiringContracts = expiringContracts,
            RecentActivities = recentAuditRows.Select(audit =>
                new RecentContractActivityResponse(
                    audit.ContractAuditId,
                    audit.ContractId,
                    audit.ContractCodeSnapshot ?? $"#{audit.ContractId}",
                    audit.ActionType,
                    ResolveActorDisplayName(
                        audit.ActorDisplayNameSnapshot,
                        audit.ActorEmployeeId,
                        audit.ActorCustomerAccessSessionId,
                        audit.ActorType),
                    audit.OccurredAt))
                .ToList()
        };
    }

    private static DashboardRange NormalizeRange(
        DashboardFilterRequest filter,
        DateTime now)
    {
        if (filter.ExpiryDays is < 1 or > 365)
        {
            throw new ArgumentException("ExpiryDays phải nằm trong khoảng 1 đến 365.");
        }

        var fromUtc = filter.From?.UtcDateTime ?? now.Date.AddDays(-29);
        var toUtc = filter.To?.UtcDateTime ?? now;
        if (fromUtc > toUtc || toUtc - fromUtc > TimeSpan.FromDays(366 * 5))
        {
            throw new ArgumentException("Khoảng thời gian dashboard không hợp lệ.");
        }

        var duration = toUtc - fromUtc;
        return new DashboardRange(
            fromUtc,
            toUtc,
            fromUtc - duration - TimeSpan.FromTicks(1),
            duration);
    }

    private static IReadOnlyList<DashboardSummaryItemResponse> BuildSummary(
        IEnumerable<byte> current,
        IEnumerable<byte> previous,
        int expiringCount)
    {
        var currentStatuses = current.ToList();
        var previousStatuses = previous.ToList();
        return
        [
            Summary("total", currentStatuses, previousStatuses, _ => true),
            Summary("drafting", currentStatuses, previousStatuses, status =>
                status is (byte)ContractStatus.Draft
                    or (byte)ContractStatus.Negotiating),
            Summary("pendingApproval", currentStatuses, previousStatuses,
                status => status == (byte)ContractStatus.PendingApproval),
            Summary("pendingSignature", currentStatuses, previousStatuses,
                status => status == (byte)ContractStatus.PendingSignature),
            Summary("signed", currentStatuses, previousStatuses,
                status => status == (byte)ContractStatus.Signed),
            Summary("completedRejected", currentStatuses, previousStatuses,
                status => status is (byte)ContractStatus.Completed
                    or (byte)ContractStatus.Rejected),
            new DashboardSummaryItemResponse("expiring", expiringCount)
        ];
    }

    private static DashboardSummaryItemResponse Summary(
        string key,
        IReadOnlyCollection<byte> current,
        IReadOnlyCollection<byte> previous,
        Func<byte, bool> predicate) =>
        new(key, current.Count(predicate), previous.Count(predicate));

    private static IReadOnlyList<DashboardVolumePointResponse> BuildVolumeSeries(
        IReadOnlyCollection<ContractMetricRow> rows,
        DashboardRange range)
    {
        var mode = range.Duration.TotalDays switch
        {
            <= 45 => VolumePeriod.Day,
            <= 240 => VolumePeriod.Week,
            _ => VolumePeriod.Month
        };

        return rows
            .GroupBy(row => FormatPeriod(row.CreatedDate, mode))
            .OrderBy(group => group.Key)
            .Select(group => new DashboardVolumePointResponse(
                group.Key,
                group.Count()))
            .ToList();
    }

    private static string FormatPeriod(DateTime value, VolumePeriod mode)
    {
        if (mode == VolumePeriod.Day)
        {
            return value.ToString("yyyy-MM-dd");
        }

        if (mode == VolumePeriod.Month)
        {
            return value.ToString("yyyy-MM");
        }

        var day = value.Date.AddDays(-(((int)value.DayOfWeek + 6) % 7));
        return day.ToString("yyyy-MM-dd");
    }

    private static string NormalizeCurrency(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim().ToUpperInvariant();

    private static string GetStatusName(byte status) =>
        Enum.IsDefined(typeof(ContractStatus), status)
            ? ((ContractStatus)status).ToString()
            : $"Unknown-{status}";

    private static string? ResolveActorDisplayName(
        string? snapshot,
        int? employeeId,
        int? customerSessionId,
        string actorType)
    {
        if (!string.IsNullOrWhiteSpace(snapshot))
        {
            return snapshot;
        }

        if (employeeId.HasValue)
        {
            return $"Nhân viên #{employeeId.Value}";
        }

        if (customerSessionId.HasValue)
        {
            return "Khách hàng";
        }

        return string.IsNullOrWhiteSpace(actorType) ? null : actorType;
    }

    private sealed record ContractMetricRow(
        byte Status,
        decimal TotalAmount,
        string CurrencyCode,
        DateTime CreatedDate);

    private sealed record DashboardRange(
        DateTime FromUtc,
        DateTime ToUtc,
        DateTime PreviousFromUtc,
        TimeSpan Duration);

    private enum VolumePeriod
    {
        Day,
        Week,
        Month
    }
}
