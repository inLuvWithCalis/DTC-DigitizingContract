using System.Security.Cryptography;
using System.Text;
using ContractManagement.API.Domains.CustomerAccess;
using Microsoft.Extensions.Options;

namespace ContractManagement.Tests.Domains.CustomerAccess;

public sealed class CustomerOtpSmtpTests
{
    private static CustomerOtpOptions CreateOptions() => new()
    {
        Provider = "Smtp",
        HashKey = Convert.ToBase64String(new byte[32]),
        EncryptionKey = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray()),
        Smtp = new CustomerOtpSmtpOptions
        {
            Username = "sender@gmail.com",
            FromAddress = "sender@gmail.com",
            AppPassword = "test-only-not-a-real-password"
        }
    };

    [Fact]
    public void EmailTemplate_UsesEmbeddedDtcLogoInsteadOfEmojiIcon()
    {
        var html = CustomerOtpEmailTemplate.BuildBody(
            "012345",
            DateTime.UtcNow.AddMinutes(5));

        Assert.Contains(
            $"src=\"cid:{CustomerOtpEmailTemplate.LogoContentId}\"",
            html);
        Assert.DoesNotContain("🔐", html);
    }

    [Fact]
    public void EncryptedPayload_RoundTripsRecipientSnapshotAndExpiry()
    {
        var crypto = new CustomerAccessCryptography(Options.Create(CreateOptions()));
        var message = new CustomerOtpDeliveryMessage("+84912345678", "012345",
            "customer@example.test", DateTime.UtcNow.AddMinutes(5));
        var encrypted = crypto.EncryptDeliveryPayload(message);
        Assert.Equal(message, crypto.DecryptDeliveryPayload(encrypted));
        Assert.DoesNotContain(message.EmailAddress!, encrypted);
        Assert.DoesNotContain(message.Otp, encrypted);
    }

    [Fact]
    public void LegacyEncryptedPayload_RemainsReadableWithoutInventingRecipient()
    {
        var options = CreateOptions();
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes("+84912345678\n012345");
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Convert.FromBase64String(options.EncryptionKey!), 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        var payload = Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
        var crypto = new CustomerAccessCryptography(Options.Create(options));
        Assert.Equal(new CustomerOtpDeliveryMessage("+84912345678", "012345"),
            crypto.DecryptDeliveryPayload(payload));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@example.test,b@example.test")]
    [InlineData("a@example.test\r\nBcc: b@example.test")]
    public async Task Smtp_RejectsInvalidRecipientBeforeConnecting(string? recipient)
    {
        var provider = new SmtpCustomerOtpDeliveryProvider(Options.Create(CreateOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DeliverAsync(
            new CustomerOtpDeliveryMessage("+84912345678", "012345", recipient,
                DateTime.UtcNow.AddMinutes(5)), CancellationToken.None));
    }

    [Fact]
    public async Task Smtp_RejectsExpiredCodeBeforeConnecting()
    {
        var provider = new SmtpCustomerOtpDeliveryProvider(Options.Create(CreateOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DeliverAsync(
            new CustomerOtpDeliveryMessage("+84912345678", "012345", "customer@example.test",
                DateTime.UtcNow.AddSeconds(-1)), CancellationToken.None));
    }

    [Fact]
    public void Smtp_RequiresCredentialsAndStartTlsPort()
    {
        var smtp = CreateOptions().Smtp;
        Assert.True(smtp.IsConfigured());
        smtp.Port = 465;
        Assert.False(smtp.IsConfigured());
        smtp.Port = 587;
        smtp.AppPassword = "";
        Assert.False(smtp.IsConfigured());
    }
}
