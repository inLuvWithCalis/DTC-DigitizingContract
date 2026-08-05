namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractCustomerVerificationPhoneResponse
{
    public int VerificationPhoneId { get; init; }

    public string PhoneSource { get; init; } = string.Empty;

    public string MaskedPhoneNumber { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }

    public DateTime CreatedDate { get; init; }

    public string RowVersion { get; init; } = string.Empty;
}

public sealed class ContractCustomerAccessLinkResponse
{
    public int LinkId { get; init; }

    public string State { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    /// <summary>Returned only from a successful create or replace operation.</summary>
    public string PublicUrl { get; init; } = string.Empty;
}
