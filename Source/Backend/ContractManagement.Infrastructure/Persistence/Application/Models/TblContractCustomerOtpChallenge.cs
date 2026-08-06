namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// One-use public OTP challenge. OtpHash is a keyed hash, never a plaintext code.
/// </summary>
public sealed class TblContractCustomerOtpChallenge
{
    public int CustomerOtpChallengeId { get; set; }

    public string PublicChallengeId { get; set; } = null!;

    public int LinkId { get; set; }

    public int VerificationPhoneId { get; set; }

    public string Purpose { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public int FailedAttemptCount { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime? InvalidatedAt { get; set; }

    public DateTime CreatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
