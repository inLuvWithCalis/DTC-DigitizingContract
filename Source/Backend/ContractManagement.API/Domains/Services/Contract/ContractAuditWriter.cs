using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ContractManagement.Domains.Services.Contract;

/// <summary>
/// Stages non-secret Contract audit facts for employee, customer, and system actors.
/// </summary>
public sealed class ContractAuditWriter : IContractAuditWriter
{
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxCorrelationIdLength = 100;
    private const int MaxReasonLength = 1000;
    private const int MaxSafeStringLength = 500;

    private static readonly HashSet<string> SubjectTypes =
    [
        ContractAuditSubjectTypes.Contract,
        ContractAuditSubjectTypes.ContractVersion,
        ContractAuditSubjectTypes.NegotiationComment,
        ContractAuditSubjectTypes.CustomerAccessLink,
        ContractAuditSubjectTypes.CustomerOtpChallenge,
        ContractAuditSubjectTypes.CustomerAccessSession,
        ContractAuditSubjectTypes.ApprovalRequest
    ];

    /*
     * Audit values deliberately use a small, action-specific vocabulary. This
     * makes an accidental addition of a phone, token, comment body, or snapshot
     * fail fast instead of silently persisting sensitive data.
     */
    private static readonly IReadOnlyDictionary<string, HashSet<string>>
        AllowedValueKeysByAction = new Dictionary<string, HashSet<string>>(
            StringComparer.Ordinal)
        {
            [ContractAuditActionTypes.ContractCreated] = ContractFields(),
            [ContractAuditActionTypes.ResponsibleAssigned] = ContractFields(),
            [ContractAuditActionTypes.ResponsibilityTransferred] = ContractFields(),
            [ContractAuditActionTypes.DraftUpdated] = ContractFields(),
            [ContractAuditActionTypes.ApprovalSubmitted] = ApprovalFields(),
            [ContractAuditActionTypes.ApprovalApproved] = ApprovalFields(),
            [ContractAuditActionTypes.ApprovalReturned] = ApprovalFields(),
            [ContractAuditActionTypes.ApprovalRejected] = ApprovalFields(),
            [ContractAuditActionTypes.ApprovalWithdrawn] = ApprovalFields(),
            [ContractAuditActionTypes.ContractAttachmentUploaded] =
                AttachmentFields(),
            [ContractAuditActionTypes.ContractAttachmentDeleted] =
                AttachmentFields(),
            [ContractAuditActionTypes.NegotiationStarted] = ContractFields(),
            [ContractAuditActionTypes.NegotiationRoundCreated] =
                Fields("SourceVersionId", "NewVersionId", "CurrentVersionId",
                    "SourceVersionLocked", "ItemCount", "TermCount", "TotalAmount",
                    "CarriedForwardThreadCount", "CarriedForwardCommentCount"),
            [ContractAuditActionTypes.ExternalFeedbackCreated] = CommentFields(),
            [ContractAuditActionTypes.NegotiationReplyCreated] = CommentFields(),
            [ContractAuditActionTypes.NegotiationCommentResolved] = CommentFields(),
            [ContractAuditActionTypes.NegotiationCommentReopened] = CommentFields(),
            [ContractAuditActionTypes.NegotiationCommentCarriedForward] =
                Fields("SourceCommentId", "SourceVersionId", "NewCommentId",
                    "NewVersionId", "Target", "TermId", "ParentCommentId", "State"),
            [ContractAuditActionTypes.CustomerCommentCreated] = CommentFields(),
            [ContractAuditActionTypes.CustomerCommentReplyCreated] = CommentFields(),
            [ContractAuditActionTypes.VerificationPhoneSelected] =
                VerificationPhoneFields(),
            [ContractAuditActionTypes.VerificationPhoneChanged] =
                VerificationPhoneFields(),
            [ContractAuditActionTypes.CustomerAccessLinkCreated] = LinkFields(),
            [ContractAuditActionTypes.CustomerAccessLinkReplaced] = LinkFields(),
            [ContractAuditActionTypes.CustomerAccessLinkRevoked] = LinkFields(),
            [ContractAuditActionTypes.CustomerAccessLinkActivated] = LinkFields(),
            [ContractAuditActionTypes.CustomerAccessLinkInvalidated] = LinkFields(),
            [ContractAuditActionTypes.CustomerOtpRequested] = OtpFields(),
            [ContractAuditActionTypes.CustomerOtpSent] = OtpFields(),
            [ContractAuditActionTypes.CustomerOtpFailed] = OtpFields(),
            [ContractAuditActionTypes.CustomerOtpLocked] = OtpFields(),
            [ContractAuditActionTypes.CustomerOtpVerified] = OtpFields(),
            [ContractAuditActionTypes.CustomerSessionCreated] =
                Fields("CustomerAccessSessionId", "SessionState", "IdleExpiresAt", "HardExpiresAt"),
            [ContractAuditActionTypes.CustomerSessionRevoked] =
                Fields("CustomerAccessSessionId", "SessionState", "RevocationReasonCode"),
            [ContractAuditActionTypes.PublicVersionViewed] =
                Fields("CurrentVersionId", "SessionState"),
            [ContractAuditActionTypes.PublicAccessDenied] =
                Fields("LinkId", "CurrentVersionId", "LinkState", "SessionState"),
            [ContractAuditActionTypes.ConcurrencyConflict] = ContractFields()
        };

    private readonly DbDtctechContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContractAuditWriter(
        DbDtctechContext dbContext,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public void StageEmployeeAudits(
        IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        StageAudits(requests.Select(request => new ContractAuditWriteRequest(
            request.ContractId,
            request.VersionId,
            ContractAuditActorTypes.Employee,
            request.ActorEmployeeId,
            null,
            request.ActionType,
            request.Result,
            request.OccurredAt,
            request.PreviousContractStatus,
            request.NewContractStatus,
            request.PreviousResponsibleEmployeeId,
            request.NewResponsibleEmployeeId,
            request.Reason,
            request.SubjectType,
            request.SubjectId,
            request.PreviousValues,
            request.NewValues,
            request.FailureCode,
            request.CorrelationId)).ToList());
    }

    public void StageAudits(
        IReadOnlyCollection<ContractAuditWriteRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            return;
        }

        var tenantId = _currentTenant.GetRequiredTenant().TenantId;
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = NormalizeAndLimit(
            httpContext?.Connection.RemoteIpAddress?.ToString(),
            MaxIpAddressLength);
        var userAgent = NormalizeAndLimit(
            httpContext?.Request.Headers.UserAgent.ToString(),
            MaxUserAgentLength);

        var audits = requests.Select(request =>
        {
            ValidateActor(request);
            var subjectType = request.SubjectType
                ?? ContractAuditSubjectTypes.Contract;
            var subjectId = request.SubjectId ?? request.ContractId;
            ValidateSubject(subjectType, subjectId);
            var correlationId = NormalizeAndLimit(
                    request.CorrelationId,
                    MaxCorrelationIdLength)
                ?? NormalizeAndLimit(
                    httpContext?.TraceIdentifier,
                    MaxCorrelationIdLength)
                ?? Guid.NewGuid().ToString("N");

            return new TblContractAudit
            {
                TenantId = tenantId,
                ContractId = request.ContractId,
                VersionId = request.VersionId,
                SubjectType = subjectType,
                SubjectId = subjectId,
                ActorType = request.ActorType,
                ActorEmployeeId = request.ActorEmployeeId,
                ActorCustomerAccessSessionId =
                    request.ActorCustomerAccessSessionId,
                ActionType = request.ActionType,
                Result = request.Result,
                PreviousContractStatus = request.PreviousContractStatus,
                NewContractStatus = request.NewContractStatus,
                PreviousResponsibleEmployeeId =
                    request.PreviousResponsibleEmployeeId,
                NewResponsibleEmployeeId = request.NewResponsibleEmployeeId,
                Reason = SanitizeReason(request.Reason),
                PreviousValuesJson = SerializeSafeValues(
                    request.ActionType,
                    request.PreviousValues),
                NewValuesJson = SerializeSafeValues(
                    request.ActionType,
                    request.NewValues),
                FailureCode = NormalizeCode(request.FailureCode),
                OccurredAt = request.OccurredAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = correlationId
            };
        }).ToList();

        _dbContext.TblContractAudits.AddRange(audits);
    }

    private static void ValidateActor(ContractAuditWriteRequest request)
    {
        var isEmployee = string.Equals(
            request.ActorType,
            ContractAuditActorTypes.Employee,
            StringComparison.Ordinal);
        var isCustomer = string.Equals(
            request.ActorType,
            ContractAuditActorTypes.Customer,
            StringComparison.Ordinal);
        var isSystem = string.Equals(
            request.ActorType,
            ContractAuditActorTypes.System,
            StringComparison.Ordinal);

        if ((!isEmployee && !isCustomer && !isSystem)
            || (isEmployee && (request.ActorEmployeeId is not > 0
                || request.ActorCustomerAccessSessionId.HasValue))
            || (isCustomer && (request.ActorEmployeeId.HasValue
                || request.ActorCustomerAccessSessionId is not > 0))
            || (isSystem && (request.ActorEmployeeId.HasValue
                || request.ActorCustomerAccessSessionId.HasValue)))
        {
            throw new InvalidOperationException("Contract audit actor is invalid.");
        }
    }

    private static void ValidateSubject(string subjectType, int subjectId)
    {
        if (!SubjectTypes.Contains(subjectType) || subjectId <= 0)
        {
            throw new InvalidOperationException("Contract audit subject is invalid.");
        }
    }

    private static string? SerializeSafeValues(
        string actionType,
        IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        if (!AllowedValueKeysByAction.TryGetValue(
                actionType,
                out var allowedKeys))
        {
            throw new InvalidOperationException(
                "Contract audit action does not permit before/after values.");
        }

        var safeValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            if (!allowedKeys.Contains(key) || !IsSafeScalar(value))
            {
                throw new InvalidOperationException(
                    "Contract audit value is not allowed for this action.");
            }

            safeValues[key] = value is string text
                ? NormalizeSafeString(text)
                : value;
        }

        return JsonSerializer.Serialize(safeValues);
    }

    private static bool IsSafeScalar(object? value) => value is null
        or bool or byte or short or int or long or decimal or double or float
        or DateTime or DateTimeOffset or Guid
        || value is string text && text.Length <= MaxSafeStringLength;

    private static string? NormalizeCode(string? value)
    {
        var normalized = NormalizeAndLimit(value, 64);
        if (normalized is null)
        {
            return null;
        }

        if (!Regex.IsMatch(normalized, "^[A-Za-z][A-Za-z0-9]*$"))
        {
            throw new InvalidOperationException("Contract audit failure code is invalid.");
        }

        return normalized;
    }

    private static string? SanitizeReason(string? value)
    {
        var normalized = NormalizeAndLimit(value, MaxReasonLength);
        if (normalized is null)
        {
            return null;
        }

        var withoutUrls = Regex.Replace(
            normalized,
            "https?://\\S+",
            "[redacted-url]",
            RegexOptions.IgnoreCase);
        var withoutSecrets = Regex.Replace(
            withoutUrls,
            "(?i)\\b(otp|token|cookie|session)\\s*[:=]\\s*\\S+",
            "$1=[redacted]");
        return Regex.Replace(
            withoutSecrets,
            "(?<!\\d)(?:\\+?\\d[\\d\\s-]{7,}\\d)(?!\\d)",
            "[redacted-phone]");
    }

    private static string NormalizeSafeString(string value)
    {
        var normalized = value.Trim();
        return normalized.Length <= MaxSafeStringLength
            ? normalized
            : normalized[..MaxSafeStringLength];
    }

    private static HashSet<string> ContractFields() => Fields(
        "Status", "ResponsibleEmployeeId", "CurrentVersionId", "CustomerId",
        "CustomerName", "ContractName", "ContractNameEn", "EffectiveDate",
        "ExpireDate", "CurrencyCode", "Subtotal", "TotalDiscount", "TotalVat",
        "TotalAmount", "ItemCount", "TermCount", "AddedItems", "UpdatedItems",
        "RemovedItems", "AddedTerms", "UpdatedTerms", "RemovedTerms",
        "ContractType", "LanguageMode", "TemplateVersionId", "ParentContractId");

    private static HashSet<string> ApprovalFields() => Fields(
        "Status", "CurrentVersionId", "VersionLocked", "ApprovalRequestId",
        "ApprovalStatus", "WorkflowId", "SnapshotSchemaVersion",
        "TemplateVersionId", "SnapshotHash", "DocxFileId", "DocxHash",
        "PdfFileId", "PdfHash", "ArtifactCount", "InvalidatedLinkCount",
        "RevokedSessionCount", "ResolvedByEmployeeId");

    private static HashSet<string> AttachmentFields() => Fields(
        "AttachmentId", "FileId", "FileName", "DocumentType", "UploadDate");

    private static HashSet<string> VerificationPhoneFields() => Fields(
        "VerificationPhoneId", "VerificationPhoneMasked", "PhoneSource",
        "LinkId", "LinkState");

    private static HashSet<string> CommentFields() => Fields(
        "Source", "Target", "TermId", "ParentCommentId", "State");

    private static HashSet<string> LinkFields() => Fields(
        "VerificationPhoneId", "LinkId", "PreviousLinkId", "NewLinkId",
        "CurrentVersionId", "ExpiresAt", "LinkState");

    private static HashSet<string> OtpFields() => Fields(
        "LinkId", "CustomerOtpChallengeId", "CurrentVersionId", "ExpiresAt",
        "ChallengeState", "FailedAttemptCount");

    private static HashSet<string> Fields(params string[] fields) =>
        new(fields, StringComparer.Ordinal);

    private static string? NormalizeAndLimit(
        string? value,
        int maxLength)
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
