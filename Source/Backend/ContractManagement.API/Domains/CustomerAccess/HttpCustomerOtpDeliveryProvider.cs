using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace ContractManagement.API.Domains.CustomerAccess;

/// <summary>
/// Minimal provider adapter for staging sandbox and production delivery accounts.
/// Provider-specific routing remains configuration-owned.
/// </summary>
public sealed class HttpCustomerOtpDeliveryProvider : ICustomerOtpDeliveryProvider
{
    private readonly HttpClient _httpClient;
    private readonly CustomerOtpOptions _options;

    public HttpCustomerOtpDeliveryProvider(
        HttpClient httpClient,
        IOptions<CustomerOtpOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task DeliverAsync(
        CustomerOtpDeliveryMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ProviderEndpoint)
            || string.IsNullOrWhiteSpace(_options.ProviderApiKey))
        {
            throw new InvalidOperationException("OTP delivery provider is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            _options.ProviderEndpoint)
        {
            Content = JsonContent.Create(new
            {
                phoneNumber = message.PhoneNumberNormalized,
                otp = message.Otp
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.ProviderApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
