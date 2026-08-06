using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Public;

public sealed class CreateCustomerNegotiationCommentRequest
{
    public int? TermId { get; set; }

    public int? ParentCommentId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
}
