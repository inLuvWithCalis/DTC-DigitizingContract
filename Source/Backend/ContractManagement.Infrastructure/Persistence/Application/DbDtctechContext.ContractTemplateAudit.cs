using System.Text.Json;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    private void ValidateContractTemplateAuditEntries()
    {
        var changedEntries = ChangeTracker
            .Entries<TblContractTemplateAudit>()
            .Where(entry => entry.State == EntityState.Modified
                || entry.State == EntityState.Deleted)
            .ToList();
        if (changedEntries.Count > 0)
        {
            throw new InvalidOperationException(
                "Template audit là dữ liệu append-only và không được sửa hoặc xóa.");
        }

        foreach (var entry in ChangeTracker
                     .Entries<TblContractTemplateAudit>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            ValidateNewContractTemplateAudit(entry.Entity);
        }
    }

    private static void ValidateNewContractTemplateAudit(
        TblContractTemplateAudit audit)
    {
        if (audit.TenantId <= 0 || audit.TemplateId <= 0
            || audit.TemplateVersionId <= 0 || audit.ActorEmployeeId <= 0
            || string.IsNullOrWhiteSpace(audit.ActionType)
            || string.IsNullOrWhiteSpace(audit.Result)
            || string.IsNullOrWhiteSpace(audit.CorrelationId)
            || audit.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("Template audit không hợp lệ.");
        }

        ValidateTemplateAuditJsonObject(audit.PreviousValuesJson, "PreviousValuesJson");
        ValidateTemplateAuditJsonObject(audit.NewValuesJson, "NewValuesJson");
    }

    private static void ValidateTemplateAuditJsonObject(string? json, string fieldName)
    {
        if (json is null)
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Template audit {fieldName} must be a JSON object.");
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Template audit {fieldName} is invalid JSON.", exception);
        }
    }
}
