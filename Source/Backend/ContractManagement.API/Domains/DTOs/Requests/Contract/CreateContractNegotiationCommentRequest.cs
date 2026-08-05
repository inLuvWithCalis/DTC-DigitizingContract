using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public class CreateContractNegotiationCommentRequest
{
    [Range(1, int.MaxValue)]
    public int CurrentVersionId { get; set; }

    public int? TermId { get; set; }

    public int? ParentCommentId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Alias dùng cho endpoint external-feedback.
/// Actor và source luôn do service xác định từ session/business rule.
/// </summary>
public sealed class CreateExternalFeedbackRequest
    : CreateContractNegotiationCommentRequest
{
}
