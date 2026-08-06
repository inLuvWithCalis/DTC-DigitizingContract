namespace ContractManagement.API.Domains.CustomerAccess;

public sealed record CustomerOtpDeliveryMessage(
    string PhoneNumberNormalized,
    string Otp);

public interface ICustomerOtpDeliveryProvider
{
    Task DeliverAsync(
        CustomerOtpDeliveryMessage message,
        CancellationToken cancellationToken);
}
