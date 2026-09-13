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

        if (ChangeTracker.Entries<TblContractTemplateAuditValue>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Template audit value là dữ liệu append-only và không được sửa hoặc xóa.");
        }

        foreach (var entry in ChangeTracker
                     .Entries<TblContractTemplateAudit>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            ValidateNewContractTemplateAudit(entry.Entity);
        }

        foreach (var entry in ChangeTracker
                     .Entries<TblContractTemplateAuditValue>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            ValidateContractTemplateAuditValue(entry.Entity);
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

    }

    private static void ValidateContractTemplateAuditValue(
        TblContractTemplateAuditValue value)
    {
        if (!Enum.IsDefined(value.ValueSide)
            || value.FieldCode is < 1 or > 11)
        {
            throw new InvalidOperationException(
                "Template audit value metadata không hợp lệ.");
        }

        var populated = (value.IntegerValue.HasValue ? 1 : 0)
            + (value.LongValue.HasValue ? 1 : 0)
            + (value.StringValue is not null ? 1 : 0);
        if (value.IsNull)
        {
            if (populated != 0)
            {
                throw new InvalidOperationException(
                    "Template audit null không được chứa typed value.");
            }
            return;
        }

        var correctColumn = value.FieldCode switch
        {
            1 or 5 or 6 or 9 => value.IntegerValue.HasValue,
            3 or 7 or 10 => value.LongValue.HasValue,
            2 or 4 or 8 or 11 => value.StringValue is { Length: <= 32 },
            _ => false
        };
        if (populated != 1 || !correctColumn)
        {
            throw new InvalidOperationException(
                "Template audit typed value không hợp lệ.");
        }
    }
}
