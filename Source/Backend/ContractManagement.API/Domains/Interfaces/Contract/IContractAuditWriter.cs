namespace ContractManagement.Domains.Interfaces.Contract;

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
    public const string NegotiationRoundCreated =
        "NegotiationRoundCreated";
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
    string? Reason = null);

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
    string? Reason = null);

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
