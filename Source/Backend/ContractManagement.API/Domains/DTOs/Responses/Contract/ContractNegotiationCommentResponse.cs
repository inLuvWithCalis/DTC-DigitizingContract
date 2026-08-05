using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractNegotiationCommentResponse
{
    public int CommentId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public int? TermId { get; set; }

    public int? ParentCommentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string Source { get; set; } = "ExternalFeedback";

    public bool ExternalFeedback { get; set; } = true;

    public int RecordedByEmployeeId { get; set; }

    public int CreatedEmployeeId
    {
        get => RecordedByEmployeeId;
        set => RecordedByEmployeeId = value;
    }

    public ContractNegotiationCommentState State { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public List<ContractNegotiationCommentEventResponse> Events { get; set; } = [];
}

public sealed class ContractNegotiationCommentEventResponse
{
    public int CommentEventId { get; set; }

    public int CommentId { get; set; }

    public ContractNegotiationCommentEventType EventType { get; set; }

    public int EmployeeId { get; set; }

    public DateTime OccurredAt { get; set; }
}
