namespace ContractManagement.API.Domains.CustomerAccess;

public sealed record CustomerOtpDeliveryMessage(
    string PhoneNumberNormalized,
    string Otp,
    string? EmailAddress = null,
    DateTime? ExpiresAt = null);

public interface ICustomerOtpDeliveryProvider
{
    Task DeliverAsync(
        CustomerOtpDeliveryMessage message,
        CancellationToken cancellationToken);
}
