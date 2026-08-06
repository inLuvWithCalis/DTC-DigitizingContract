namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Durable encrypted OTP delivery work item. Recipient and OTP are encrypted together.
/// </summary>
public sealed class TblContractCustomerOtpDeliveryOutbox
{
    public int CustomerOtpDeliveryOutboxId { get; set; }

    public int ChallengeId { get; set; }

    public string EncryptedPayload { get; set; } = null!;

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
