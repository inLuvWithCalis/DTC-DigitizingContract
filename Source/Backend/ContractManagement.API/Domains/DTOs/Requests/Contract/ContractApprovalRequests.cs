using System.ComponentModel.DataAnnotations;
using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public sealed class ContractApprovalInboxFilterRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [MaxLength(200)]
    public string? Keyword { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

public sealed class ContractApprovalDecisionRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public sealed class ContractApprovalBulkDecisionItemRequest
{
    [Range(1, int.MaxValue)]
    public int ApprovalRequestId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractApprovalBulkDecisionRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<ContractApprovalBulkDecisionItemRequest> Items { get; set; }
        = [];

    public ApprovalRequestStatus Decision { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public sealed class WithdrawContractApprovalRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
