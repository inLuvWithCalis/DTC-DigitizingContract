namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Append-only lifecycle event của một negotiation comment.
/// </summary>
public partial class TblContractNegotiationCommentEvent
{
    public int CommentEventId { get; set; }

    public int CommentId { get; set; }

    /// <summary>
    /// 1 = Created, 2 = Resolved, 3 = Reopened.
    /// </summary>
    public byte EventType { get; set; }

    public string ActorType { get; set; } = null!;

    public int? EmployeeId { get; set; }

    public int? CustomerAccessSessionId { get; set; }

    public DateTime OccurredAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime CreatedDate
    {
        get => OccurredAt;
        set => OccurredAt = value;
    }
}
