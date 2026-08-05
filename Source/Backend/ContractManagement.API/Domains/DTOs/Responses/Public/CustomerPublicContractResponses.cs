namespace ContractManagement.API.Domains.DTOs.Responses.Public;

public sealed class CustomerOtpRequestAcceptedResponse
{
    public string PublicChallengeId { get; init; } = string.Empty;
}

public sealed class CustomerSharedContractResponse
{
    public string? ContractCode { get; init; }

    public string ContractName { get; init; } = string.Empty;

    public string? ContractNameEn { get; init; }

    public DateTime? EffectiveDate { get; init; }

    public DateTime? ExpireDate { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }

    public List<CustomerPublicContractItemResponse> Items { get; init; } = [];

    public List<CustomerPublicContractTermResponse> Terms { get; init; } = [];

    public List<CustomerPublicNegotiationCommentResponse> Comments { get; init; } = [];
}

public sealed class CustomerPublicContractItemResponse
{
    public string ItemName { get; init; } = string.Empty;

    public string? ItemNameEn { get; init; }

    public string? ItemDescription { get; init; }

    public decimal Quantity { get; init; }

    public string? UnitName { get; init; }

    public decimal LineTotal { get; init; }

    public int DisplayOrder { get; init; }
}

public sealed class CustomerPublicContractTermResponse
{
    public int TermId { get; init; }

    public string TermCode { get; init; } = string.Empty;

    public string TermTitle { get; init; } = string.Empty;

    public string? TermTitleEn { get; init; }

    public string? TermContent { get; init; }

    public string? TermContentEn { get; init; }

    public bool IsNegotiable { get; init; }

    public int DisplayOrder { get; init; }
}

public sealed class CustomerPublicNegotiationCommentResponse
{
    public int CommentId { get; init; }

    public int? TermId { get; init; }

    public int? ParentCommentId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string LifecycleState { get; init; } = string.Empty;

    public DateTime CreatedDate { get; init; }

    public DateTime? UpdatedDate { get; init; }
}
