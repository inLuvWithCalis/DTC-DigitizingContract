namespace ContractManagement.API.Domains.CustomerAccess;

/// <summary>
/// Development-only delivery sink. OTPs are written to the backend console so
/// the public customer flow can be tested without a real SMS provider.
/// </summary>
internal sealed class FakeCustomerOtpDeliveryProvider(
    ILogger<FakeCustomerOtpDeliveryProvider> logger)
    : ICustomerOtpDeliveryProvider
{
    public Task DeliverAsync(
        CustomerOtpDeliveryMessage message,
        CancellationToken cancellationToken)
    {
        var phoneSuffix = message.PhoneNumberNormalized.Length <= 4
            ? message.PhoneNumberNormalized
            : message.PhoneNumberNormalized[^4..];

        logger.LogWarning(
            "DEVELOPMENT ONLY - Customer OTP {Otp} for phone ending {PhoneSuffix}",
            message.Otp,
            phoneSuffix);

        return Task.CompletedTask;
    }
}
