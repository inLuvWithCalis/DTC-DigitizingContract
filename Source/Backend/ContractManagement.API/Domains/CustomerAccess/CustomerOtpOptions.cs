namespace ContractManagement.API.Domains.CustomerAccess;

public sealed class CustomerOtpOptions
{
    public const string SectionName = "CustomerOtp";

    public string? HashKey { get; set; }

    public string? EncryptionKey { get; set; }

    public string Provider { get; set; } = "Fake";

    public string? ProviderEndpoint { get; set; }

    public string? ProviderApiKey { get; set; }

    public int MaxDeliveryAttempts { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 30;

    public CustomerOtpSmtpOptions Smtp { get; set; } = new();

    public bool UsesSmtp => string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase);
}
