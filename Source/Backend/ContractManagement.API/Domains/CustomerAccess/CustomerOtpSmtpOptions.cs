using System.Net.Mail;

namespace ContractManagement.API.Domains.CustomerAccess;

public sealed class CustomerOtpSmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? AppPassword { get; set; }
    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "Contract Management";
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Host)
        && Port == 587
        && IsEmailAddress(Username)
        && IsEmailAddress(FromAddress)
        && !string.IsNullOrWhiteSpace(AppPassword)
        && TimeoutSeconds is >= 1 and <= 45;

    public static bool IsEmailAddress(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains('\r') && !value.Contains('\n')
        && MailAddress.TryCreate(value, out var address)
        && string.Equals(address.Address, value, StringComparison.Ordinal);
}
