using System.Text.Json;
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

    private static readonly HashSet<string> SafeValueKeys =
    [
        "DocumentFileId",
        "DocumentExtension",
        "DocumentSizeBytes",
        "ValidationStatus",
        "RecognizedPlaceholderCount",
        "PreviewFileId",
        "PreviewSizeBytes",
        "PreviewStatus",
        "PublishedPreviewPdfFileId",
        "PublishedPreviewPdfSizeBytes",
        "PublishStatus"
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
            return new TblContractTemplateAudit
            {
                TenantId = tenantId,
                TemplateId = request.TemplateId,
                TemplateVersionId = request.TemplateVersionId,
                ActorEmployeeId = request.ActorEmployeeId,
                ActionType = request.ActionType,
                Result = request.Result,
                FailureCode = NormalizeFailureCode(request.FailureCode),
                PreviousValuesJson = SerializeSafeValues(request.PreviousValues),
                NewValuesJson = SerializeSafeValues(request.NewValues),
                OccurredAt = request.OccurredAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = Limit(request.CorrelationId,
                                    MaxCorrelationIdLength)
                    ?? Limit(httpContext?.TraceIdentifier, MaxCorrelationIdLength)
                    ?? Guid.NewGuid().ToString("N")
            };
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

    private static string? SerializeSafeValues(
        IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            if (!SafeValueKeys.Contains(key) || !IsSafeValue(key, value))
            {
                throw new InvalidOperationException(
                    "Template audit value không nằm trong safelist.");
            }

            normalized[key] = value;
        }

        return JsonSerializer.Serialize(normalized);
    }

    private static bool IsSafeValue(string key, object? value) => key switch
    {
        "DocumentFileId" => value is null || value is int fileId && fileId > 0,
        "DocumentExtension" => value is "doc" or "docx" or "docm" or "dotx"
            or "dotm" or "other",
        "DocumentSizeBytes" => value is long size && size >= 0,
        "ValidationStatus" => value is "Valid" or "Invalid" or "Unchanged",
        "RecognizedPlaceholderCount" => value is int count && count >= 0
            && count <= SoftwareSupplyPlaceholderCatalog.GetAll().Count,
        "PreviewFileId" => value is null || value is int previewFileId
            && previewFileId > 0,
        "PreviewSizeBytes" => value is long previewSize && previewSize >= 0,
        "PreviewStatus" => value is "Current" or "Rejected" or "Stale"
            or "Unchanged",
        "PublishedPreviewPdfFileId" => value is null || value is int pdfFileId
            && pdfFileId > 0,
        "PublishedPreviewPdfSizeBytes" => value is long pdfSize && pdfSize >= 0,
        "PublishStatus" => value is "Draft" or "Published" or "Retired"
            or "Unchanged",
        _ => false
    };

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
