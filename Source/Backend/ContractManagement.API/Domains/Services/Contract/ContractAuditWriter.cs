using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
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
        ContractAuditSubjectTypes.ApprovalRequest,
        ContractAuditSubjectTypes.SignedEvidence,
        ContractAuditSubjectTypes.ContractAppendix,
        ContractAuditSubjectTypes.AcceptanceEvidence,
        ContractAuditSubjectTypes.Payment
    ];

    /*
     * Audit values deliberately use a small, action-specific vocabulary. This
     * makes an accidental addition of a phone, token, comment body, or snapshot
     * fail fast instead of silently persisting sensitive data.
     */
    private static readonly IReadOnlyDictionary<string, HashSet<ContractAuditFieldCode>>
        AllowedValueKeysByAction = new Dictionary<string, HashSet<ContractAuditFieldCode>>(
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
            [ContractAuditActionTypes.SignedEvidenceUploaded] =
                SignedEvidenceFields(),
            [ContractAuditActionTypes.SignedEvidenceSuperseded] =
                SignedEvidenceFields(),
            [ContractAuditActionTypes.AcceptanceEvidenceUploaded] =
                Fields("AcceptanceEvidenceId", "FileId", "FileType", "Sha256", "CurrentVersionId"),
            [ContractAuditActionTypes.PaymentAdded] = PaymentFields(),
            [ContractAuditActionTypes.PaymentVoided] = PaymentFields(),
            [ContractAuditActionTypes.PaymentMilestoneAnchored] =
                Fields("PaymentMilestoneId", "CurrentVersionId", "AnchorDate", "DueDate"),
            [ContractAuditActionTypes.PaymentMilestoneStatusChanged] =
                Fields("PaymentMilestoneId", "CurrentVersionId", "PaymentStatus", "PaidAt", "PaidByEmployeeId"),
            [ContractAuditActionTypes.ContractCompleted] =
                Fields("Status", "CurrentVersionId", "TotalAmount", "PaidAmount"),
            [ContractAuditActionTypes.ContractAttachmentUploaded] =
                AttachmentFields(),
            [ContractAuditActionTypes.ContractAttachmentDeleted] =
                AttachmentFields(),
            [ContractAuditActionTypes.NegotiationStarted] = ContractFields(),
            [ContractAuditActionTypes.NegotiationRoundCreated] =
                Fields("SourceVersionId", "NewVersionId", "CurrentVersionId",
                    "SourceVersionLocked", "ItemCount", "TermCount", "AppendixCount", "TotalAmount",
                    "CarriedForwardThreadCount", "CarriedForwardCommentCount"),
            [ContractAuditActionTypes.ContractAppendixSelected] =
                Fields("CurrentVersionId", "AppendixCount"),
            [ContractAuditActionTypes.ContractAppendixRemoved] =
                Fields("CurrentVersionId", "AppendixCount"),
            [ContractAuditActionTypes.ContractAppendixTermCreated] =
                Fields("CurrentVersionId", "AppendixCount"),
            [ContractAuditActionTypes.ContractAppendixTermUpdated] =
                Fields("CurrentVersionId", "AppendixCount"),
            [ContractAuditActionTypes.ContractAppendixTermDeleted] =
                Fields("CurrentVersionId", "AppendixCount"),
            [ContractAuditActionTypes.ContractAppendixTermsReordered] =
                Fields("CurrentVersionId", "AppendixCount"),
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
            var previousContractStatus = ResolveHeaderByte(
                request.PreviousContractStatus,
                request.PreviousValues,
                ContractAuditFieldCode.Status);
            var newContractStatus = ResolveHeaderByte(
                request.NewContractStatus,
                request.NewValues,
                ContractAuditFieldCode.Status);
            var previousResponsibleEmployeeId = ResolveHeaderInt32(
                request.PreviousResponsibleEmployeeId,
                request.PreviousValues,
                ContractAuditFieldCode.ResponsibleEmployeeId);
            var newResponsibleEmployeeId = ResolveHeaderInt32(
                request.NewResponsibleEmployeeId,
                request.NewValues,
                ContractAuditFieldCode.ResponsibleEmployeeId);

            var audit = new TblContractAudit
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
                PreviousContractStatus = previousContractStatus,
                NewContractStatus = newContractStatus,
                PreviousResponsibleEmployeeId =
                    previousResponsibleEmployeeId,
                NewResponsibleEmployeeId = newResponsibleEmployeeId,
                Reason = SanitizeReason(request.Reason),
                FailureCode = NormalizeCode(request.FailureCode),
                OccurredAt = request.OccurredAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = correlationId
            };

            foreach (var value in CreateSafeValues(
                         request.ActionType,
                         AuditValueSide.Previous,
                         request.PreviousValues))
            {
                audit.Values.Add(value);
            }

            foreach (var value in CreateSafeValues(
                         request.ActionType,
                         AuditValueSide.New,
                         request.NewValues))
            {
                audit.Values.Add(value);
            }

            return audit;
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

    private static IReadOnlyCollection<TblContractAuditValue> CreateSafeValues(
        string actionType,
        AuditValueSide side,
        IReadOnlyCollection<ContractAuditValueInput>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        if (!AllowedValueKeysByAction.TryGetValue(
                actionType,
                out var allowedKeys))
        {
            throw new InvalidOperationException(
                "Contract audit action does not permit before/after values.");
        }

        var safeValues = new List<TblContractAuditValue>(values.Count);
        var seen = new HashSet<ContractAuditFieldCode>();
        foreach (var value in values)
        {
            if (!allowedKeys.Contains(value.FieldCode)
                || !seen.Add(value.FieldCode)
                || value.ValueKind != ContractAuditFieldKinds.Get(value.FieldCode)
                || !IsValidValue(value))
            {
                throw new InvalidOperationException(
                    "Contract audit value is not allowed for this action.");
            }

            if (value.FieldCode is ContractAuditFieldCode.Status
                or ContractAuditFieldCode.ResponsibleEmployeeId)
            {
                continue;
            }

            safeValues.Add(new TblContractAuditValue
            {
                ValueSide = side,
                FieldCode = (short)value.FieldCode,
                ValueKind = value.ValueKind,
                IsNull = value.IsNull,
                IntegerValue = value.IntegerValue,
                DecimalValue = value.DecimalValue,
                StringValue = value.StringValue is null
                    ? null
                    : NormalizeSafeString(value.StringValue),
                DateTimeValue = value.DateTimeValue,
                BooleanValue = value.BooleanValue
            });
        }

        return safeValues;
    }

    private static byte? ResolveHeaderByte(
        byte? explicitValue,
        IReadOnlyCollection<ContractAuditValueInput>? values,
        ContractAuditFieldCode fieldCode)
    {
        var input = values?.SingleOrDefault(value =>
            value.FieldCode == fieldCode);
        if (input is null)
        {
            return explicitValue;
        }

        var inputValue = input.IsNull
            ? null
            : checked((byte?)input.IntegerValue);
        if (explicitValue.HasValue && explicitValue != inputValue)
        {
            throw new InvalidOperationException(
                $"Contract audit header {fieldCode} is inconsistent.");
        }

        return explicitValue ?? inputValue;
    }

    private static int? ResolveHeaderInt32(
        int? explicitValue,
        IReadOnlyCollection<ContractAuditValueInput>? values,
        ContractAuditFieldCode fieldCode)
    {
        var input = values?.SingleOrDefault(value =>
            value.FieldCode == fieldCode);
        if (input is null)
        {
            return explicitValue;
        }

        var inputValue = input.IsNull
            ? null
            : checked((int?)input.IntegerValue);
        if (explicitValue.HasValue && explicitValue != inputValue)
        {
            throw new InvalidOperationException(
                $"Contract audit header {fieldCode} is inconsistent.");
        }

        return explicitValue ?? inputValue;
    }

    private static bool IsValidValue(ContractAuditValueInput value)
    {
        var populated = (value.IntegerValue.HasValue ? 1 : 0)
            + (value.DecimalValue.HasValue ? 1 : 0)
            + (value.StringValue is not null ? 1 : 0)
            + (value.DateTimeValue.HasValue ? 1 : 0)
            + (value.BooleanValue.HasValue ? 1 : 0);
        if (value.IsNull)
        {
            return populated == 0;
        }

        if (populated != 1)
        {
            return false;
        }

        return value.ValueKind switch
        {
            AuditScalarValueKind.Integer => value.IntegerValue.HasValue,
            AuditScalarValueKind.Decimal => value.DecimalValue.HasValue,
            AuditScalarValueKind.String => value.StringValue is not null
                && value.StringValue.Length <= MaxSafeStringLength,
            AuditScalarValueKind.DateTime => value.DateTimeValue.HasValue,
            AuditScalarValueKind.Boolean => value.BooleanValue.HasValue,
            _ => false
        };
    }

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

    private static HashSet<ContractAuditFieldCode> ContractFields() => Fields(
        "Status", "ResponsibleEmployeeId", "CurrentVersionId", "CustomerId",
        "CustomerName", "ContractName", "ContractNameEn", "EffectiveDate",
        "ExpireDate", "CurrencyCode", "Subtotal", "TotalDiscount", "TotalVat",
        "TotalAmount", "ItemCount", "TermCount", "AppendixCount", "AddedItems", "UpdatedItems",
        "RemovedItems", "AddedTerms", "UpdatedTerms", "RemovedTerms",
        "ContractType", "LanguageMode", "TemplateVersionId", "ParentContractId");

    private static HashSet<ContractAuditFieldCode> ApprovalFields() => Fields(
        "Status", "CurrentVersionId", "VersionLocked", "ApprovalRequestId",
        "ApprovalStatus", "WorkflowId",
        "TemplateVersionId", "SnapshotHash", "DocxFileId", "DocxHash",
        "PdfFileId", "PdfHash", "ArtifactCount", "InvalidatedLinkCount",
        "RevokedSessionCount", "ResolvedByEmployeeId");

    private static HashSet<ContractAuditFieldCode> AttachmentFields() => Fields(
        "AttachmentId", "FileId", "FileName", "DocumentType", "UploadDate");

    private static HashSet<ContractAuditFieldCode> SignedEvidenceFields() => Fields(
        "Status", "CurrentVersionId", "SignedEvidenceId", "FileId",
        "FileType", "Sha256", "EvidenceStatus", "SupersedesEvidenceId",
        "SignDate");

    private static HashSet<ContractAuditFieldCode> PaymentFields() => Fields(
        "ContractPaymentId", "PaymentMilestoneId", "CurrentVersionId", "PaymentDate", "Amount",
        "CurrencyCode", "PaymentMethod", "ReferenceCode", "EvidenceFileId",
        "PaymentStatus", "PaidAmount", "RemainingAmount");

    private static HashSet<ContractAuditFieldCode> VerificationPhoneFields() => Fields(
        "VerificationPhoneId", "VerificationPhoneMasked", "PhoneSource",
        "LinkId", "LinkState");

    private static HashSet<ContractAuditFieldCode> CommentFields() => Fields(
        "Source", "Target", "TermId", "ParentCommentId", "State");

    private static HashSet<ContractAuditFieldCode> LinkFields() => Fields(
        "VerificationPhoneId", "LinkId", "PreviousLinkId", "NewLinkId",
        "CurrentVersionId", "ExpiresAt", "LinkState");

    private static HashSet<ContractAuditFieldCode> OtpFields() => Fields(
        "LinkId", "CustomerOtpChallengeId", "CurrentVersionId", "ExpiresAt",
        "ChallengeState", "FailedAttemptCount");

    private static HashSet<ContractAuditFieldCode> Fields(params string[] fields) =>
        fields.Select(field =>
        {
            if (!Enum.TryParse<ContractAuditFieldCode>(field, false, out var code))
            {
                throw new InvalidOperationException(
                    $"Unknown contract audit field '{field}'.");
            }

            return code;
        }).ToHashSet();

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
