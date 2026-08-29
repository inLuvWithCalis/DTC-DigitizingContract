namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public static class ContractApprovalReadinessCodes
{
    public const string ContractNotNegotiating = "ContractNotNegotiating";
    public const string CurrentVersionLocked = "CurrentVersionLocked";
    public const string CurrentVersionAlreadyShared = "CurrentVersionAlreadyShared";
    public const string CurrentVersionNotShared = "CurrentVersionNotShared";
    public const string ActiveCustomerAccessLinkRequired =
        "ActiveCustomerAccessLinkRequired";
    public const string OpenNegotiationCommentsExist =
        "OpenNegotiationCommentsExist";
    public const string ContractCodeRequired = "ContractCodeRequired";
    public const string ContractNameRequired = "ContractNameRequired";
    public const string ContractItemRequired = "ContractItemRequired";
    public const string ContractTermRequired = "ContractTermRequired";
    public const string InvalidContractDateRange = "InvalidContractDateRange";
    public const string ContractTotalMismatch = "ContractTotalMismatch";
    public const string BilingualContractNameRequired =
        "BilingualContractNameRequired";
    public const string BilingualItemNameRequired =
        "BilingualItemNameRequired";
    public const string BilingualTermTitleRequired =
        "BilingualTermTitleRequired";
}

public sealed class ContractApprovalReadinessResponse
{
    public bool CanSubmit { get; init; }

    /// <summary>
    /// Version hiện hành đã từng được kích hoạt cho khách hàng xem.
    /// Giá trị này không quay lại false khi link hết hạn hoặc bị thu hồi.
    /// </summary>
    public bool HasEverBeenShared { get; init; }

    public bool HasActiveCurrentVersionLink { get; init; }

    public int OpenCommentCount { get; init; }

    public List<ContractApprovalReadinessBlockerResponse> Blockers { get; init; }
        = [];
}

public sealed class ContractApprovalReadinessBlockerResponse
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
