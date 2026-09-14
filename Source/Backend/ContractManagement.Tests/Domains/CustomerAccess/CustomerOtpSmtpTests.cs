using System.Security.Cryptography;
using System.Net.Mime;
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
    public void SmtpMessage_EmbedsLogoInTheHtmlViewWithoutAttachmentMetadata()
    {
        var options = CreateOptions();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        using var mail = SmtpCustomerOtpDeliveryProvider.BuildMailMessage(
            new CustomerOtpDeliveryMessage(
                "+84912345678",
                "012345",
                "customer@example.test",
                expiresAt),
            expiresAt,
            options.Smtp,
            [1, 2, 3, 4]);

        Assert.False(mail.IsBodyHtml);
        Assert.Contains("Mã xác thực (OTP) của bạn", mail.Body);
        Assert.Empty(mail.Attachments);

        var htmlView = Assert.Single(mail.AlternateViews);
        Assert.Equal(MediaTypeNames.Text.Html, htmlView.ContentType.MediaType);
        var logo = Assert.Single(htmlView.LinkedResources);
        Assert.Equal(CustomerOtpEmailTemplate.LogoContentId, logo.ContentId);
        Assert.Equal(
            $"cid:{CustomerOtpEmailTemplate.LogoContentId}",
            logo.ContentLink!.AbsoluteUri);
        Assert.Equal(MediaTypeNames.Image.Png, logo.ContentType.MediaType);
        Assert.Null(logo.ContentType.Name);
        Assert.Equal(TransferEncoding.Base64, logo.TransferEncoding);
    }

    [Fact]
    public void EncryptedScalars_RoundTripIndependently()
    {
        var crypto = new CustomerAccessCryptography(Options.Create(CreateOptions()));
        var phone = crypto.EncryptScalar("+84912345678");
        var otp = crypto.EncryptScalar("012345");
        var email = crypto.EncryptScalar("customer@example.test");

        Assert.Equal("+84912345678", crypto.DecryptScalar(phone));
        Assert.Equal("012345", crypto.DecryptScalar(otp));
        Assert.Equal("customer@example.test", crypto.DecryptScalar(email));
        Assert.NotEqual(phone, otp);
    }

    [Fact]
    public void EncryptedScalar_RejectsTamperedEnvelope()
    {
        var crypto = new CustomerAccessCryptography(Options.Create(CreateOptions()));
        var envelope = crypto.EncryptScalar("012345");
        envelope[^1] ^= 0xff;

        Assert.Throws<AuthenticationTagMismatchException>(() =>
            crypto.DecryptScalar(envelope));
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
