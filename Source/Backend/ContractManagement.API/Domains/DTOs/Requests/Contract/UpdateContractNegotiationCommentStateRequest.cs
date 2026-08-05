using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public class UpdateContractNegotiationCommentStateRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ResolveContractNegotiationCommentRequest
    : UpdateContractNegotiationCommentStateRequest
{
}

public sealed class ReopenContractNegotiationCommentRequest
    : UpdateContractNegotiationCommentStateRequest
{
}
