namespace ContractManagement.Domains.Interfaces.Contract;

public static class ContractAuditActorTypes
{
    public const string Employee = "Employee";
}

public static class ContractAuditActionTypes
{
    public const string ContractCreated = "ContractCreated";
    public const string ResponsibleAssigned = "ResponsibleAssigned";
    public const string ResponsibilityTransferred =
        "ResponsibilityTransferred";
    public const string NegotiationRoundCreated =
        "NegotiationRoundCreated";
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

/// <summary>
/// Stage Contract audit vào DbContext hiện tại.
/// Writer không lưu database hoặc tự quản lý transaction.
/// </summary>
public interface IContractAuditWriter
{
    void StageEmployeeAudits(
        IReadOnlyCollection<EmployeeContractAuditWriteRequest> requests);
}
