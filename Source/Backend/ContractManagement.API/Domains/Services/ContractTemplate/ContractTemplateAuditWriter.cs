using System.Text.RegularExpressions;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>
/// Writes only non-sensitive, allow-listed Template DOCX audit metadata.
/// </summary>
public sealed class ContractTemplateAuditWriter : IContractTemplateAuditWriter
{
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxCorrelationIdLength = 100;

    private static readonly HashSet<string> ActionTypes =
    [
        ContractTemplateAuditActionTypes.DocumentUploaded,
        ContractTemplateAuditActionTypes.DocumentReplaced,
        ContractTemplateAuditActionTypes.ValidationInvalid,
        ContractTemplateAuditActionTypes.ValidationRejected,
        ContractTemplateAuditActionTypes.ConcurrencyConflict,
        ContractTemplateAuditActionTypes.PreviewGenerated,
        ContractTemplateAuditActionTypes.PreviewRejected,
        ContractTemplateAuditActionTypes.PreviewConcurrencyConflict,
        ContractTemplateAuditActionTypes.TemplateVersionPublished,
        ContractTemplateAuditActionTypes.TemplateVersionRetired,
        ContractTemplateAuditActionTypes.PdfRenderFailed,
        ContractTemplateAuditActionTypes.PublishConcurrencyConflict
    ];

    private static readonly HashSet<string> Results =
    [
        ContractTemplateAuditResults.Succeeded,
        ContractTemplateAuditResults.Invalid,
        ContractTemplateAuditResults.Rejected,
        ContractTemplateAuditResults.Conflict
    ];

    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContractTemplateAuditWriter(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public void StageAudits(
        IReadOnlyCollection<ContractTemplateAuditWriteRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0)
        {
            return;
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = Limit(httpContext?.Connection.RemoteIpAddress?.ToString(),
            MaxIpAddressLength);
        var userAgent = Limit(httpContext?.Request.Headers.UserAgent.ToString(),
            MaxUserAgentLength);

        var records = requests.Select(request =>
        {
            ValidateRequest(request);
            var audit = new TblContractTemplateAudit
            {
                TenantId = tenantId,
                TemplateId = request.TemplateId,
                TemplateVersionId = request.TemplateVersionId,
                ActorEmployeeId = request.ActorEmployeeId,
                ActionType = request.ActionType,
                Result = request.Result,
                FailureCode = NormalizeFailureCode(request.FailureCode),
                OccurredAt = request.OccurredAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = Limit(request.CorrelationId,
                                    MaxCorrelationIdLength)
                    ?? Limit(httpContext?.TraceIdentifier, MaxCorrelationIdLength)
                    ?? Guid.NewGuid().ToString("N")
            };

            foreach (var value in CreateSafeValues(
                         AuditValueSide.Previous,
                         request.PreviousValues))
            {
                audit.Values.Add(value);
            }

            foreach (var value in CreateSafeValues(
                         AuditValueSide.New,
                         request.NewValues))
            {
                audit.Values.Add(value);
            }

            return audit;
        }).ToList();

        _dbContext.TblContractTemplateAudits.AddRange(records);
    }

    private static void ValidateRequest(ContractTemplateAuditWriteRequest request)
    {
        if (request.TemplateId <= 0 || request.TemplateVersionId <= 0
            || request.ActorEmployeeId <= 0)
        {
            throw new InvalidOperationException(
                "Template audit phải có định danh template, version và actor hợp lệ.");
        }

        if (!ActionTypes.Contains(request.ActionType)
            || !Results.Contains(request.Result)
            || request.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("Template audit request không hợp lệ.");
        }
    }

    private static IReadOnlyCollection<TblContractTemplateAuditValue>
        CreateSafeValues(
            AuditValueSide side,
            IReadOnlyCollection<ContractTemplateAuditValueInput>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = new List<TblContractTemplateAuditValue>(values.Count);
        var seen = new HashSet<ContractTemplateAuditFieldCode>();
        foreach (var value in values)
        {
            if (!seen.Add(value.FieldCode) || !IsSafeValue(value))
            {
                throw new InvalidOperationException(
                    "Template audit value không nằm trong safelist.");
            }

            normalized.Add(new TblContractTemplateAuditValue
            {
                ValueSide = side,
                FieldCode = (byte)value.FieldCode,
                IsNull = value.IsNull,
                IntegerValue = value.IntegerValue,
                LongValue = value.LongValue,
                StringValue = value.StringValue
            });
        }

        return normalized;
    }

    private static bool IsSafeValue(ContractTemplateAuditValueInput value)
    {
        if (value.IsNull)
        {
            return value.IntegerValue is null && value.LongValue is null
                && value.StringValue is null
                && value.FieldCode is ContractTemplateAuditFieldCode.DocumentFileId
                    or ContractTemplateAuditFieldCode.PreviewFileId
                    or ContractTemplateAuditFieldCode.PublishedPreviewPdfFileId;
        }

        return value.FieldCode switch
        {
            ContractTemplateAuditFieldCode.DocumentFileId
                or ContractTemplateAuditFieldCode.PreviewFileId
                or ContractTemplateAuditFieldCode.PublishedPreviewPdfFileId =>
                value.IntegerValue is > 0 && value.LongValue is null
                && value.StringValue is null,
            ContractTemplateAuditFieldCode.DocumentSizeBytes
                or ContractTemplateAuditFieldCode.PreviewSizeBytes
                or ContractTemplateAuditFieldCode.PublishedPreviewPdfSizeBytes =>
                value.LongValue is >= 0 && value.IntegerValue is null
                && value.StringValue is null,
            ContractTemplateAuditFieldCode.RecognizedPlaceholderCount =>
                value.IntegerValue is >= 0 and <= 10_000
                && value.LongValue is null && value.StringValue is null,
            ContractTemplateAuditFieldCode.DocumentExtension =>
                value.IntegerValue is null && value.LongValue is null
                && value.StringValue is "doc" or "docx" or "docm" or "dotx"
                    or "dotm" or "other",
            ContractTemplateAuditFieldCode.ValidationStatus =>
                value.IntegerValue is null && value.LongValue is null
                && value.StringValue is "Valid" or "Invalid" or "Unchanged",
            ContractTemplateAuditFieldCode.PreviewStatus =>
                value.IntegerValue is null && value.LongValue is null
                && value.StringValue is "Current" or "Rejected" or "Stale"
                    or "Unchanged",
            ContractTemplateAuditFieldCode.PublishStatus =>
                value.IntegerValue is null && value.LongValue is null
                && value.StringValue is "Draft" or "Published" or "Retired"
                    or "Unchanged",
            _ => false
        };
    }

    private static string? NormalizeFailureCode(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.Trim();
        if (!Regex.IsMatch(normalized, "^[A-Za-z][A-Za-z0-9]{0,63}$"))
        {
            throw new InvalidOperationException("Template audit failure code không hợp lệ.");
        }

        return normalized;
    }

    private static string? Limit(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
