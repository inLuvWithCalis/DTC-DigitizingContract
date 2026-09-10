using System.Text.Json;
using System.Text.RegularExpressions;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ContractManagement.Domains.Services.ContractTemplate;

public sealed class ContractPlaceholderDefinitionService(
    DbDtctechContext db, ContractPlaceholderCatalog catalog, IContractPlaceholderSourceRegistry registry)
{
    public async Task AuthorizeAsync(int employeeId, CancellationToken ct)
    {
        if (!await db.TblEmployees.AsNoTracking().AnyAsync(x => x.EmployeeId == employeeId &&
                x.Status == 1 && x.EmployeeType == (byte)EmployeeType.AdminOfficer, ct))
            throw new UnauthorizedAccessException("Chỉ AdminOfficer đang hoạt động được quản lý placeholder.");
    }

    private void EnsureEnabled()
    {
        if (!catalog.CustomEnabled)
            throw new PlaceholderOperationException("CustomPlaceholdersDisabled", "Placeholder tùy chỉnh chưa được bật cho doanh nghiệp này.");
    }

    public async Task<SoftwareSupplyPlaceholderDefinition> SaveAsync(int? id, SaveContractPlaceholderRequest request, int actor, CancellationToken ct)
    {
        await AuthorizeAsync(actor, ct);
        EnsureEnabled();
        var key = request.PlaceholderKey.Trim();
        if (key.StartsWith("{{") && key.EndsWith("}}")) key = key[2..^2].Trim();
        key = key.ToUpperInvariant();
        if (key.Length > 100 || !Regex.IsMatch(key, "^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$", RegexOptions.CultureInvariant))
            throw new PlaceholderOperationException("PlaceholderKeyInvalid", "Key chỉ gồm chữ in hoa, số và dấu gạch dưới; bắt đầu bằng chữ.");
        if (ContractPlaceholderCatalog.SystemDefinitions.Any(x => x.Key == key))
            throw new PlaceholderOperationException("SystemPlaceholderImmutable", "Không thể thay đổi hoặc dùng trùng key hệ thống.");
        if (string.IsNullOrWhiteSpace(request.FieldLabel) || request.FieldLabel.Length > 300 || request.DefaultValue?.Length > 2000)
            throw new PlaceholderOperationException("PlaceholderKeyInvalid", "Tên hiển thị hoặc giá trị mặc định không hợp lệ.");
        var sourceKey = request.SourceFieldKey.Trim();
        var format = string.IsNullOrWhiteSpace(request.FormatString) ? null : request.FormatString.Trim();
        registry.Format(sourceKey, null, format, null); // validates source and format even if no data
        var entity = id.HasValue ? await FindAsync(id.Value, ct) : new TblContractPlaceholderDefinition
        {
            PlaceholderKey = key, CreatedEmployeeId = actor, CreatedDate = DateTime.UtcNow
        };
        var previous = id.HasValue ? AuditValues(entity) : null;
        if (id.HasValue)
        {
            CheckRowVersion(entity, request.RowVersion);
            if (entity.PlaceholderKey != key)
                throw new PlaceholderOperationException("PlaceholderKeyInvalid", "Không thể đổi key. Hãy tạo placeholder mới.");
        }
        if (await db.TblContractPlaceholderDefinitions.AnyAsync(x => x.PlaceholderKey == key && x.PlaceholderDefinitionId != entity.PlaceholderDefinitionId, ct))
            throw new PlaceholderOperationException("PlaceholderKeyAlreadyExists", "Key placeholder đã tồn tại.");
        entity.FieldLabel = request.FieldLabel.Trim();
        entity.SourceFieldKey = sourceKey;
        entity.DefaultValue = request.DefaultValue;
        entity.FormatString = format;
        entity.UpdatedEmployeeId = actor;
        entity.UpdatedDate = DateTime.UtcNow;
        if (!id.HasValue) db.TblContractPlaceholderDefinitions.Add(entity);
        StageAudit(entity, actor, id.HasValue ? "PlaceholderDefinitionUpdated" : "PlaceholderDefinitionCreated", previous);
        await SaveChangesAsync(ct);
        ContractPlaceholderMetrics.Mutations.Add(1, new KeyValuePair<string, object?>("action", id.HasValue ? "update" : "create"));
        return catalog.ToDefinition(entity);
    }

    public async Task<SoftwareSupplyPlaceholderDefinition> SetActiveAsync(int id, bool active, string rowVersion, int actor, CancellationToken ct)
    {
        await AuthorizeAsync(actor, ct);
        EnsureEnabled();
        var entity = await FindAsync(id, ct);
        CheckRowVersion(entity, rowVersion);
        registry.GetRequired(entity.SourceFieldKey);
        var previous = AuditValues(entity);
        entity.IsActive = active;
        entity.UpdatedEmployeeId = actor;
        entity.UpdatedDate = DateTime.UtcNow;
        StageAudit(entity, actor, active ? "PlaceholderDefinitionActivated" : "PlaceholderDefinitionDeactivated", previous);
        await SaveChangesAsync(ct);
        ContractPlaceholderMetrics.Mutations.Add(1, new KeyValuePair<string, object?>("action", active ? "activate" : "deactivate"));
        return catalog.ToDefinition(entity);
    }

    public async Task<IReadOnlyList<PlaceholderUsage>> UsageAsync(int id, int actor, CancellationToken ct)
    {
        await AuthorizeAsync(actor, ct);
        var entity = await FindAsync(id, ct);
        return await (from field in db.TblContractTemplateFields.AsNoTracking()
            join version in db.TblContractTemplateVersions on field.TemplateVersionId equals version.TemplateVersionId
            join template in db.TblContractTemplates on version.TemplateId equals template.TemplateId
            where field.PlaceholderKey == entity.PlaceholderKey
            orderby template.TemplateCode, version.VersionNo descending
            select new PlaceholderUsage(template.TemplateId, version.TemplateVersionId, template.TemplateCode, version.VersionNo, version.Status)).ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, string rowVersion, int actor, CancellationToken ct)
    {
        await AuthorizeAsync(actor, ct);
        EnsureEnabled();
        var entity = await FindAsync(id, ct);
        CheckRowVersion(entity, rowVersion);
        var isUsed = await db.TblContractTemplateFields.AsNoTracking()
            .AnyAsync(x => x.PlaceholderKey == entity.PlaceholderKey, ct);
        if (isUsed)
            throw new PlaceholderOperationException("PlaceholderInUse",
                "Placeholder đã được dùng trong template và không thể xóa. Hãy ngừng sử dụng để giữ lịch sử.");

        var previous = AuditValues(entity);
        StageAudit(entity, actor, "PlaceholderDefinitionDeleted", previous);
        db.TblContractPlaceholderDefinitions.Remove(entity);
        await SaveChangesAsync(ct);
        ContractPlaceholderMetrics.Mutations.Add(1,
            new KeyValuePair<string, object?>("action", "delete"));
        return true;
    }

    private async Task<TblContractPlaceholderDefinition> FindAsync(int id, CancellationToken ct) =>
        await db.TblContractPlaceholderDefinitions.SingleOrDefaultAsync(x => x.PlaceholderDefinitionId == id, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy placeholder.");

    private void CheckRowVersion(TblContractPlaceholderDefinition entity, string? rowVersion)
    {
        byte[] expected;
        try { expected = Convert.FromBase64String(rowVersion ?? ""); }
        catch (FormatException) { throw new PlaceholderOperationException("PlaceholderConcurrencyConflict", "Phiên bản dữ liệu không hợp lệ. Hãy tải lại."); }
        if (expected.Length != 8 || !expected.SequenceEqual(entity.RowVersion))
            throw new PlaceholderOperationException("PlaceholderConcurrencyConflict", "Placeholder đã thay đổi. Hãy tải lại trước khi lưu.");
        db.Entry(entity).Property(x => x.RowVersion).OriginalValue = expected;
    }

    private static string AuditValues(TblContractPlaceholderDefinition entity) => JsonSerializer.Serialize(new
    { entity.SourceFieldKey, entity.FormatString, entity.IsActive }); // Excludes labels/defaults and resolved PII.

    private void StageAudit(TblContractPlaceholderDefinition entity, int actor, string action, string? previous) =>
        db.TblContractPlaceholderAudits.Add(new()
        {
            PlaceholderKey = entity.PlaceholderKey, ActorEmployeeId = actor, ActionType = action,
            PreviousValuesJson = previous, NewValuesJson = AuditValues(entity), OccurredAt = DateTime.UtcNow
        });

    private async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new PlaceholderOperationException("PlaceholderConcurrencyConflict", "Placeholder đã thay đổi. Hãy tải lại."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new PlaceholderOperationException("PlaceholderKeyAlreadyExists", "Key placeholder đã tồn tại."); }
    }
}
