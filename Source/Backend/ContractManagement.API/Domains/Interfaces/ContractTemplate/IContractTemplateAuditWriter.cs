using System.Globalization;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public static class ContractTemplateAuditActionTypes
{
    public const string DocumentUploaded = "DocumentUploaded";
    public const string DocumentReplaced = "DocumentReplaced";
    public const string ValidationInvalid = "ValidationInvalid";
    public const string ValidationRejected = "ValidationRejected";
    public const string ConcurrencyConflict = "ConcurrencyConflict";
    public const string PreviewGenerated = "PreviewGenerated";
    public const string PreviewRejected = "PreviewRejected";
    public const string PreviewConcurrencyConflict = "PreviewConcurrencyConflict";
    public const string TemplateVersionPublished = "TemplateVersionPublished";
    public const string TemplateVersionRetired = "TemplateVersionRetired";
    public const string PdfRenderFailed = "PdfRenderFailed";
    public const string PublishConcurrencyConflict = "PublishConcurrencyConflict";
}

public static class ContractTemplateAuditResults
{
    public const string Succeeded = "Succeeded";
    public const string Invalid = "Invalid";
    public const string Rejected = "Rejected";
    public const string Conflict = "Conflict";
}

public enum ContractTemplateAuditFieldCode : byte
{
    DocumentFileId = 1,
    DocumentExtension,
    DocumentSizeBytes,
    ValidationStatus,
    RecognizedPlaceholderCount,
    PreviewFileId,
    PreviewSizeBytes,
    PreviewStatus,
    PublishedPreviewPdfFileId,
    PublishedPreviewPdfSizeBytes,
    PublishStatus
}

public sealed record ContractTemplateAuditValueInput(
    ContractTemplateAuditFieldCode FieldCode,
    bool IsNull,
    int? IntegerValue = null,
    long? LongValue = null,
    string? StringValue = null)
{
    internal static ContractTemplateAuditValueInput Create(
        ContractTemplateAuditFieldCode fieldCode,
        object? value)
    {
        if (value is null)
        {
            return new(fieldCode, true);
        }

        return fieldCode switch
        {
            ContractTemplateAuditFieldCode.DocumentExtension
                or ContractTemplateAuditFieldCode.ValidationStatus
                or ContractTemplateAuditFieldCode.PreviewStatus
                or ContractTemplateAuditFieldCode.PublishStatus
                when value is string text =>
                new(fieldCode, false, StringValue: text),
            ContractTemplateAuditFieldCode.DocumentSizeBytes
                or ContractTemplateAuditFieldCode.PreviewSizeBytes
                or ContractTemplateAuditFieldCode.PublishedPreviewPdfSizeBytes =>
                new(fieldCode, false, LongValue:
                    Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            ContractTemplateAuditFieldCode.DocumentFileId
                or ContractTemplateAuditFieldCode.RecognizedPlaceholderCount
                or ContractTemplateAuditFieldCode.PreviewFileId
                or ContractTemplateAuditFieldCode.PublishedPreviewPdfFileId =>
                new(fieldCode, false, IntegerValue:
                    Convert.ToInt32(value, CultureInfo.InvariantCulture)),
            _ => throw new InvalidOperationException(
                $"Template audit field {fieldCode} has an invalid value type.")
        };
    }
}

public static class ContractTemplateAuditValues
{
    public static IReadOnlyCollection<ContractTemplateAuditValueInput> Create(
        params (string Key, object? Value)[] values)
    {
        var result = new List<ContractTemplateAuditValueInput>(values.Length);
        var seen = new HashSet<ContractTemplateAuditFieldCode>();
        foreach (var (key, value) in values)
        {
            if (!Enum.TryParse<ContractTemplateAuditFieldCode>(
                    key,
                    false,
                    out var field)
                || !seen.Add(field))
            {
                throw new InvalidOperationException(
                    "Template audit field is unknown or duplicated.");
            }

            result.Add(ContractTemplateAuditValueInput.Create(field, value));
        }

        return result;
    }
}

/// <summary>
/// Only allow-listed typed metadata may be placed in the before/after values.
/// </summary>
public sealed record ContractTemplateAuditWriteRequest(
    int TemplateId,
    int TemplateVersionId,
    int ActorEmployeeId,
    string ActionType,
    string Result,
    DateTime OccurredAt,
    IReadOnlyCollection<ContractTemplateAuditValueInput>? PreviousValues = null,
    IReadOnlyCollection<ContractTemplateAuditValueInput>? NewValues = null,
    string? FailureCode = null,
    string? CorrelationId = null);

/// <summary>
/// Stages Template audit in the current DbContext so business data and audit
/// commit together. The writer never calls SaveChanges or controls a transaction.
/// </summary>
public interface IContractTemplateAuditWriter
{
    void StageAudits(IReadOnlyCollection<ContractTemplateAuditWriteRequest> requests);
}
