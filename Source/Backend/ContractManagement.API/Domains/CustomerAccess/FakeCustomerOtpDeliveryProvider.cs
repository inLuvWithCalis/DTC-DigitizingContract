namespace ContractManagement.API.Domains.CustomerAccess;

/// <summary>
/// Development/test-only delivery sink. It intentionally does not expose OTPs
/// through HTTP responses or application logs.
/// </summary>
internal sealed class FakeCustomerOtpDeliveryProvider : ICustomerOtpDeliveryProvider
{
    public Task DeliverAsync(
        CustomerOtpDeliveryMessage message,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
