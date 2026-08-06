namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// A revocable, phone-bound public access link. Only a keyed token hash is stored.
/// </summary>
public sealed class TblContractCustomerAccessLink
{
    public int CustomerAccessLinkId { get; set; }

    public int TenantId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public int VerificationPhoneId { get; set; }

    public string TokenHash { get; set; } = null!;

    public int CreatedByEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? RevokedByEmployeeId { get; set; }

    public string? RevocationReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
