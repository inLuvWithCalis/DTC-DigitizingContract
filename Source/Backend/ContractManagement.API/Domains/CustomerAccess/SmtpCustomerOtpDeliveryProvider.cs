using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;

namespace ContractManagement.API.Domains.CustomerAccess;

public sealed class SmtpCustomerOtpDeliveryProvider(IOptions<CustomerOtpOptions> options)
    : ICustomerOtpDeliveryProvider
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "Email",
        "logo_light.png");

    public async Task DeliverAsync(
        CustomerOtpDeliveryMessage message,
        CancellationToken cancellationToken)
    {
        var smtp = options.Value.Smtp;
        if (!smtp.IsConfigured())
            throw new InvalidOperationException("Customer OTP SMTP is not configured.");
        if (!CustomerOtpSmtpOptions.IsEmailAddress(message.EmailAddress))
            throw new InvalidOperationException("Customer OTP recipient email is missing or invalid.");
        if (message.ExpiresAt is not { } expiresAt || expiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Customer OTP delivery has expired. Request a new code.");

        var logoBytes = File.Exists(LogoPath)
            ? await File.ReadAllBytesAsync(LogoPath, cancellationToken)
            : null;
        using var mail = BuildMailMessage(message, expiresAt, smtp, logoBytes);
        mail.To.Add(new MailAddress(message.EmailAddress!));

        // Require STARTTLS; never fall back to an unencrypted connection.
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtp.Username, smtp.AppPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(smtp.TimeoutSeconds));
        try
        {
            await client.SendMailAsync(mail, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Let the outbox retry a timeout instead of treating it as worker shutdown.
            throw new TimeoutException("Customer OTP SMTP delivery timed out.");
        }
    }

    internal static MailMessage BuildMailMessage(
        CustomerOtpDeliveryMessage message,
        DateTime expiresAt,
        CustomerOtpSmtpOptions smtp,
        byte[]? logoBytes)
    {
        // Keep only the plain-text fallback in MailMessage.Body. The HTML body
        // must live in the AlternateView that owns the linked logo; otherwise
        // some clients select a duplicate HTML body where the cid cannot resolve.
        var mail = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress!, smtp.FromName, Encoding.UTF8),
            Subject = CustomerOtpEmailTemplate.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = CustomerOtpEmailTemplate.BuildPlainTextBody(message.Otp, expiresAt),
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        var htmlView = AlternateView.CreateAlternateViewFromString(
            CustomerOtpEmailTemplate.BuildBody(message.Otp, expiresAt),
            Encoding.UTF8,
            MediaTypeNames.Text.Html);
        if (logoBytes is { Length: > 0 })
        {
            var logo = new LinkedResource(
                new MemoryStream(logoBytes, writable: false),
                MediaTypeNames.Image.Png)
            {
                ContentId = CustomerOtpEmailTemplate.LogoContentId,
                ContentLink = new Uri(
                    $"cid:{CustomerOtpEmailTemplate.LogoContentId}",
                    UriKind.Absolute),
                TransferEncoding = TransferEncoding.Base64
            };
            // A filename encourages Gmail and other clients to expose the
            // related MIME part as a downloadable attachment.
            logo.ContentType.Name = null;
            htmlView.LinkedResources.Add(logo);
        }

        mail.AlternateViews.Add(htmlView);
        return mail;
    }
}
