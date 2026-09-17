using System.Globalization;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Domains.Interfaces.Contract;

public static class ContractAuditSubjectTypes
{
    public const string Contract = "Contract";
    public const string ContractVersion = "ContractVersion";
    public const string NegotiationComment = "NegotiationComment";
    public const string CustomerAccessLink = "CustomerAccessLink";
    public const string CustomerOtpChallenge = "CustomerOtpChallenge";
    public const string CustomerAccessSession = "CustomerAccessSession";
    public const string ApprovalRequest = "ApprovalRequest";
    public const string SignedEvidence = "SignedEvidence";
    public const string ContractAppendix = "ContractAppendix";
    public const string AcceptanceEvidence = "AcceptanceEvidence";
    public const string Payment = "Payment";
}

public static class ContractAuditFailureCodes
{
    public const string InsufficientAuthority = "InsufficientAuthority";
    public const string StaleRowVersion = "StaleRowVersion";
    public const string LinkExpired = "LinkExpired";
    public const string LinkRevoked = "LinkRevoked";
    public const string OtpMismatch = "OtpMismatch";
    public const string OtpDeliveryFailed = "OtpDeliveryFailed";
    public const string OtpLocked = "OtpLocked";
    public const string OtpRateLimited = "OtpRateLimited";
    public const string VerificationPhoneMismatch = "VerificationPhoneMismatch";
    public const string ChallengeUnavailable = "ChallengeUnavailable";
    public const string SessionExpired = "SessionExpired";
    public const string SessionRevoked = "SessionRevoked";
    public const string VersionNoLongerCurrent = "VersionNoLongerCurrent";
}

public static class ContractAuditValues
{
    public static IReadOnlyCollection<ContractAuditValueInput> Create(
        params (string Key, object? Value)[] values)
    {
        var result = new List<ContractAuditValueInput>(values.Length);
        var seen = new HashSet<ContractAuditFieldCode>();
        foreach (var (key, value) in values)
        {
            if (!Enum.TryParse<ContractAuditFieldCode>(key, false, out var field)
                || !seen.Add(field))
            {
                throw new InvalidOperationException(
                    "Contract audit field is unknown or duplicated.");
            }

            result.Add(ContractAuditValueInput.Create(
                field,
                ContractAuditFieldKinds.Get(field),
                value));
        }

        return result;
    }
}

public enum ContractAuditFieldCode : short
{
    Status = 1,
    ResponsibleEmployeeId,
    CurrentVersionId,
    CustomerId,
    CustomerName,
    ContractName,
    ContractNameEn,
    EffectiveDate,
    ExpireDate,
    CurrencyCode,
    Subtotal,
    TotalDiscount,
    TotalVat,
    TotalAmount,
    ItemCount,
    TermCount,
    AddedItems,
    UpdatedItems,
    RemovedItems,
    AddedTerms,
    UpdatedTerms,
    RemovedTerms,
    ContractType,
    LanguageMode,
    TemplateVersionId,
    ParentContractId,
    VersionLocked,
    ApprovalRequestId,
    ApprovalStatus,
    WorkflowId,
    // Persisted field-code values are append-only; 31 is intentionally unassigned.
    SnapshotHash = 32,
    DocxFileId,
    DocxHash,
    PdfFileId,
    PdfHash,
    ArtifactCount,
    InvalidatedLinkCount,
    RevokedSessionCount,
    ResolvedByEmployeeId,
    AttachmentId,
    FileId,
    FileName,
    DocumentType,
    UploadDate,
    SignedEvidenceId,
    FileType,
    Sha256,
    EvidenceStatus,
    SupersedesEvidenceId,
    AcceptanceEvidenceId,
    ContractPaymentId,
    PaymentMilestoneId,
    PaymentDate,
    Amount,
    PaymentMethod,
    ReferenceCode,
    EvidenceFileId,
    PaymentStatus,
    PaidAmount,
    RemainingAmount,
    AnchorDate,
    DueDate,
    PaidAt,
    PaidByEmployeeId,
    SourceVersionId,
    NewVersionId,
    SourceVersionLocked,
    CarriedForwardThreadCount,
    CarriedForwardCommentCount,
    SourceCommentId,
    NewCommentId,
    Source,
    Target,
    TermId,
    ParentCommentId,
    State,
    VerificationPhoneId,
    VerificationPhoneMasked,
    PhoneSource,
    LinkId,
    LinkState,
    PreviousLinkId,
    NewLinkId,
    ExpiresAt,
    CustomerOtpChallengeId,
    ChallengeState,
    FailedAttemptCount,
    CustomerAccessSessionId,
    SessionState,
    IdleExpiresAt,
    HardExpiresAt,
    RevocationReasonCode,
    AppendixCount,
    SignDate
}

public sealed record ContractAuditValueInput(
    ContractAuditFieldCode FieldCode,
    AuditScalarValueKind ValueKind,
    bool IsNull,
    long? IntegerValue = null,
    decimal? DecimalValue = null,
    string? StringValue = null,
    DateTime? DateTimeValue = null,
    bool? BooleanValue = null)
{
    internal static ContractAuditValueInput Create(
        ContractAuditFieldCode fieldCode,
        AuditScalarValueKind kind,
        object? value)
    {
        if (value is null)
        {
            return new(fieldCode, kind, true);
        }

        return kind switch
        {
            AuditScalarValueKind.Integer => new(fieldCode, kind, false,
                IntegerValue: Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            AuditScalarValueKind.Decimal => new(fieldCode, kind, false,
                DecimalValue: Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
            AuditScalarValueKind.String when value is string text =>
                new(fieldCode, kind, false, StringValue: text),
            AuditScalarValueKind.DateTime when value is DateTime dateTime =>
                new(fieldCode, kind, false, DateTimeValue: dateTime),
            AuditScalarValueKind.DateTime when value is DateTimeOffset dateTimeOffset =>
                new(fieldCode, kind, false, DateTimeValue: dateTimeOffset.UtcDateTime),
            AuditScalarValueKind.Boolean when value is bool boolean =>
                new(fieldCode, kind, false, BooleanValue: boolean),
            _ => throw new InvalidOperationException(
                $"Contract audit field {fieldCode} has an invalid value type.")
        };
    }
}

internal static class ContractAuditFieldKinds
{
    internal static AuditScalarValueKind Get(ContractAuditFieldCode field) =>
        field switch
        {
            ContractAuditFieldCode.Subtotal
                or ContractAuditFieldCode.TotalDiscount
                or ContractAuditFieldCode.TotalVat
                or ContractAuditFieldCode.TotalAmount
                or ContractAuditFieldCode.Amount
                or ContractAuditFieldCode.PaidAmount
                or ContractAuditFieldCode.RemainingAmount => AuditScalarValueKind.Decimal,

            ContractAuditFieldCode.EffectiveDate
                or ContractAuditFieldCode.ExpireDate
                or ContractAuditFieldCode.UploadDate
                or ContractAuditFieldCode.SignDate
                or ContractAuditFieldCode.PaymentDate
                or ContractAuditFieldCode.AnchorDate
                or ContractAuditFieldCode.DueDate
                or ContractAuditFieldCode.PaidAt
                or ContractAuditFieldCode.ExpiresAt
                or ContractAuditFieldCode.IdleExpiresAt
                or ContractAuditFieldCode.HardExpiresAt => AuditScalarValueKind.DateTime,

            ContractAuditFieldCode.VersionLocked
                or ContractAuditFieldCode.SourceVersionLocked => AuditScalarValueKind.Boolean,

            ContractAuditFieldCode.CustomerName
                or ContractAuditFieldCode.ContractName
                or ContractAuditFieldCode.ContractNameEn
                or ContractAuditFieldCode.AddedItems
                or ContractAuditFieldCode.UpdatedItems
                or ContractAuditFieldCode.RemovedItems
                or ContractAuditFieldCode.AddedTerms
                or ContractAuditFieldCode.UpdatedTerms
                or ContractAuditFieldCode.RemovedTerms
                or ContractAuditFieldCode.CurrencyCode
                or ContractAuditFieldCode.SnapshotHash
                or ContractAuditFieldCode.DocxHash
                or ContractAuditFieldCode.PdfHash
                or ContractAuditFieldCode.FileName
                or ContractAuditFieldCode.FileType
                or ContractAuditFieldCode.Sha256
                or ContractAuditFieldCode.PaymentMethod
                or ContractAuditFieldCode.ReferenceCode
                or ContractAuditFieldCode.Source
                or ContractAuditFieldCode.Target
                or ContractAuditFieldCode.State
                or ContractAuditFieldCode.VerificationPhoneMasked
                or ContractAuditFieldCode.PhoneSource
                or ContractAuditFieldCode.LinkState
                or ContractAuditFieldCode.ChallengeState
                or ContractAuditFieldCode.SessionState
                or ContractAuditFieldCode.RevocationReasonCode => AuditScalarValueKind.String,

            _ => AuditScalarValueKind.Integer
        };
}

public static class ContractAuditActorTypes
{
    public const string Employee = "Employee";
    public const string Customer = "Customer";
    public const string System = "System";
}

public static class ContractAuditActionTypes
{
    public const string ContractCreated = "ContractCreated";
    public const string ResponsibleAssigned = "ResponsibleAssigned";
    public const string ResponsibilityTransferred =
        "ResponsibilityTransferred";
    public const string DraftUpdated = "DraftUpdated";
    public const string ApprovalSubmitted = "ApprovalSubmitted";
    public const string ApprovalApproved = "ApprovalApproved";
    public const string ApprovalReturned = "ApprovalReturned";
    public const string ApprovalRejected = "ApprovalRejected";
    public const string ApprovalWithdrawn = "ApprovalWithdrawn";
    public const string SignedEvidenceUploaded = "SignedEvidenceUploaded";
    public const string SignedEvidenceSuperseded =
        "SignedEvidenceSuperseded";
    public const string AcceptanceEvidenceUploaded = "AcceptanceEvidenceUploaded";
    public const string PaymentAdded = "PaymentAdded";
    public const string PaymentVoided = "PaymentVoided";
    public const string PaymentMilestoneAnchored = "PaymentMilestoneAnchored";
    public const string PaymentMilestoneStatusChanged = "PaymentMilestoneStatusChanged";
    public const string ContractCompleted = "ContractCompleted";
    public const string ContractAttachmentUploaded =
        "ContractAttachmentUploaded";
    public const string ContractAttachmentDeleted =
        "ContractAttachmentDeleted";
    public const string NegotiationStarted = "NegotiationStarted";
    public const string NegotiationRoundCreated =
        "NegotiationRoundCreated";
    public const string ContractAppendixSelected = "ContractAppendixSelected";
    public const string ContractAppendixRemoved = "ContractAppendixRemoved";
    public const string ContractAppendixTermCreated = "ContractAppendixTermCreated";
    public const string ContractAppendixTermUpdated = "ContractAppendixTermUpdated";
    public const string ContractAppendixTermDeleted = "ContractAppendixTermDeleted";
    public const string ContractAppendixTermsReordered = "ContractAppendixTermsReordered";
    public const string ExternalFeedbackCreated =
        "ExternalFeedbackCreated";
    public const string ExternalFeedbackRecorded =
        ExternalFeedbackCreated;
    public const string NegotiationReplyCreated =
        "NegotiationReplyCreated";
    public const string NegotiationCommentReplyCreated =
        NegotiationReplyCreated;
    public const string NegotiationCommentResolved =
        "NegotiationCommentResolved";
    public const string NegotiationCommentReopened =
        "NegotiationCommentReopened";
    public const string NegotiationCommentCarriedForward =
        "NegotiationCommentCarriedForward";
    public const string ConcurrencyConflict = "ConcurrencyConflict";
    public const string VerificationPhoneSelected = "VerificationPhoneSelected";
    public const string VerificationPhoneChanged = "VerificationPhoneChanged";
    public const string CustomerAccessLinkCreated = "CustomerAccessLinkCreated";
    public const string CustomerAccessLinkReplaced = "CustomerAccessLinkReplaced";
    public const string CustomerAccessLinkRevoked = "CustomerAccessLinkRevoked";
    public const string CustomerAccessLinkActivated = "CustomerAccessLinkActivated";
    public const string CustomerAccessLinkInvalidated = "CustomerAccessLinkInvalidated";
    public const string CustomerOtpRequested = "CustomerOtpRequested";
    public const string CustomerOtpSent = "CustomerOtpSent";
    public const string CustomerOtpFailed = "CustomerOtpFailed";
    public const string CustomerOtpLocked = "CustomerOtpLocked";
    public const string CustomerOtpVerified = "CustomerOtpVerified";
    public const string CustomerSessionCreated = "CustomerSessionCreated";
    public const string CustomerSessionRevoked = "CustomerSessionRevoked";
    public const string PublicVersionViewed = "PublicVersionViewed";
    public const string CustomerCommentCreated = "CustomerCommentCreated";
    public const string CustomerCommentReplyCreated = "CustomerCommentReplyCreated";
    public const string PublicAccessDenied = "PublicAccessDenied";
}

public static class ContractAuditResults
{
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Denied = "Denied";
    public const string RateLimited = "RateLimited";
    public const string ConcurrencyConflict = "ConcurrencyConflict";
}

/// <summary>
/// Dữ liệu business audit của một hành động do employee thực hiện.
/// </summary>
public sealed record EmployeeContractAuditWriteRequest(
    int ContractId,
    int? VersionId,
    int ActorEmployeeId,
    string ActionType,
    string Result,
    DateTime OccurredAt,
    byte? PreviousContractStatus = null,
    byte? NewContractStatus = null,
    int? PreviousResponsibleEmployeeId = null,
    int? NewResponsibleEmployeeId = null,
    string? Reason = null,
    string? SubjectType = null,
    int? SubjectId = null,
    IReadOnlyCollection<ContractAuditValueInput>? PreviousValues = null,
    IReadOnlyCollection<ContractAuditValueInput>? NewValues = null,
    string? FailureCode = null,
    string? CorrelationId = null);

public sealed record ContractAuditWriteRequest(
    int ContractId,
    int? VersionId,
    string ActorType,
    int? ActorEmployeeId,
    int? ActorCustomerAccessSessionId,
    string ActionType,
    string Result,
    DateTime OccurredAt,
    byte? PreviousContractStatus = null,
    byte? NewContractStatus = null,
    int? PreviousResponsibleEmployeeId = null,
    int? NewResponsibleEmployeeId = null,
    string? Reason = null,
    string? SubjectType = null,
    int? SubjectId = null,
    IReadOnlyCollection<ContractAuditValueInput>? PreviousValues = null,
    IReadOnlyCollection<ContractAuditValueInput>? NewValues = null,
    string? FailureCode = null,
    string? CorrelationId = null);

/// <summary>
/// Stage Contract audit vào DbContext hiện tại.
/// Writer không lưu database hoặc tự quản lý transaction.
/// </summary>
public interface IContractAuditWriter
{
    void StageAudits(
        IReadOnlyCollection<ContractAuditWriteRequest> requests);

    void StageEmployeeAudits(
        IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests);
}
