using System.Globalization;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>Stages values in the caller's transaction. Never commits independently of the contract.</summary>
public sealed class ContractPlaceholderValueService(DbDtctechContext db, IContractPlaceholderSourceRegistry registry)
    : IContractPlaceholderValueService
{
    public async Task<IReadOnlyDictionary<string, string>> CaptureAsync(TblContract contract, TblContractVersion version,
        bool refresh = false, CancellationToken cancellationToken = default)
    {
        var templateId = version.TemplateVersionId;
        var fields = await db.TblContractTemplateFields.AsNoTracking()
            .Where(x => x.TemplateVersionId == templateId && !x.IsSystem).ToListAsync(cancellationToken);
        var stored = await db.TblContractVersionPlaceholderValues
            .Where(x => x.VersionId == version.VersionId && x.ContractId == contract.ContractId).ToListAsync(cancellationToken);
        // Repeated render/capture calls in the same unit of work also see values staged before SaveChanges.
        stored = stored.Concat(db.TblContractVersionPlaceholderValues.Local.Where(x =>
                x.VersionId == version.VersionId && x.ContractId == contract.ContractId && !stored.Contains(x)))
            .Where(x => db.Entry(x).State != EntityState.Deleted).ToList();
        var complete = stored.Count == fields.Count && fields.All(f => stored.Any(v =>
            v.PlaceholderKey == f.PlaceholderKey && v.TemplateVersionId == templateId && v.SourceFieldKey == f.SourceFieldKey));
        if (complete && (!refresh || version.IsLocked)) return stored.ToDictionary(x => x.PlaceholderKey, x => x.RenderedValue, StringComparer.Ordinal);
        if (version.IsLocked)
            throw new PlaceholderOperationException("PlaceholderValueResolveFailed", "Version đã khóa thiếu snapshot placeholder. Không thể đọc lại dữ liệu hiện tại.");
        if (fields.Count == 0)
        {
            db.TblContractVersionPlaceholderValues.RemoveRange(stored);
            return new Dictionary<string, string>();
        }
        var customer = await db.TblCustomers.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == contract.CustomerId, cancellationToken)
            ?? throw new PlaceholderOperationException("PlaceholderValueResolveFailed", "Không tìm thấy khách hàng của hợp đồng.");
        // A draft may be created before the legal profile is configured. Only its fields need that source.
        var tenant = fields.Any(x => x.SourceFieldKey!.StartsWith("provider.", StringComparison.Ordinal))
            ? await db.TblTenantLegalProfiles.AsNoTracking().SingleOrDefaultAsync(cancellationToken) : null;
        TblEmployee? owner = null;
        TblDepartment? department = null;
        if (fields.Any(x => x.SourceFieldKey!.StartsWith("contract-owner.", StringComparison.Ordinal) || x.SourceFieldKey!.StartsWith("department.", StringComparison.Ordinal)))
        {
            owner = await db.TblEmployees.AsNoTracking().SingleOrDefaultAsync(x => x.EmployeeId == contract.EmployeeId, cancellationToken);
            if (owner?.DepartmentId is { } departmentId)
                department = await db.TblDepartments.AsNoTracking().SingleOrDefaultAsync(x => x.DepartmentId == departmentId, cancellationToken);
        }
        var context = new ContractPlaceholderResolveContext(contract, version, customer, tenant, owner, department);
        var keys = fields.Select(x => x.PlaceholderKey).ToHashSet(StringComparer.Ordinal);
        db.TblContractVersionPlaceholderValues.RemoveRange(stored.Where(x => !keys.Contains(x.PlaceholderKey)));
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var f in fields)
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            object? raw;
            string rendered;
            try
            {
                raw = registry.Resolve(f.SourceFieldKey!, context);
                rendered = registry.Format(f.SourceFieldKey!, raw, f.FormatString, f.DefaultValue);
                ContractPlaceholderMetrics.Resolved.Add(1);
                if (raw is null || raw is string text && string.IsNullOrWhiteSpace(text))
                    ContractPlaceholderMetrics.Defaulted.Add(1);
            }
            catch
            {
                ContractPlaceholderMetrics.Failures.Add(1, new KeyValuePair<string, object?>("source", f.SourceFieldKey));
                throw;
            }
            finally
            {
                ContractPlaceholderMetrics.ResolveDuration.Record(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
            if (f.IsRequired && string.IsNullOrWhiteSpace(rendered))
                throw new PlaceholderOperationException("PlaceholderValueRequired", $"Placeholder {f.PlaceholderKey} thiếu dữ liệu.");
            var row = stored.SingleOrDefault(x => x.PlaceholderKey == f.PlaceholderKey);
            if (row is null)
            {
                row = new() { ContractId = contract.ContractId, VersionId = version.VersionId, PlaceholderKey = f.PlaceholderKey };
                db.TblContractVersionPlaceholderValues.Add(row);
            }
            row.TemplateVersionId = templateId;
            row.SourceFieldKey = f.SourceFieldKey!;
            row.RawValue = raw is IFormattable fmt ? fmt.ToString(null, CultureInfo.InvariantCulture) : raw?.ToString();
            row.RenderedValue = rendered;
            row.CapturedDate = DateTime.UtcNow;
            result.Add(f.PlaceholderKey, rendered);
        }
        return result;
    }
}
