namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Durable OTP delivery work item. Every sensitive scalar uses an independent
/// authenticated binary envelope; no plaintext or aggregate JSON is persisted.
/// </summary>
public sealed class TblContractCustomerOtpDeliveryOutbox
{
    public int CustomerOtpDeliveryOutboxId { get; set; }

    public int ChallengeId { get; set; }

    public byte[] PhoneCiphertext { get; set; } = null!;

    public byte[] OtpCiphertext { get; set; } = null!;

    public byte[]? EmailCiphertext { get; set; }

    public DateTime DeliveryExpiresAt { get; set; }

    public string Status { get; set; } = null!;

    public int AttemptCount { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public DateTime? LeaseUntil { get; set; }

    public string? LeaseId { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public string? LastFailure { get; set; }

    public DateTime CreatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
