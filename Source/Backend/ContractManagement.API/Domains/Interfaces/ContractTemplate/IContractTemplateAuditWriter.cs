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

/// <summary>
/// Only allow-listed metadata may be placed in the before/after dictionaries.
/// </summary>
public sealed record ContractTemplateAuditWriteRequest(
    int TemplateId,
    int TemplateVersionId,
    int ActorEmployeeId,
    string ActionType,
    string Result,
    DateTime OccurredAt,
    IReadOnlyDictionary<string, object?>? PreviousValues = null,
    IReadOnlyDictionary<string, object?>? NewValues = null,
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
