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

        using var mail = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress!, smtp.FromName, Encoding.UTF8),
            Subject = CustomerOtpEmailTemplate.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = CustomerOtpEmailTemplate.BuildBody(message.Otp, expiresAt),
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        mail.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                CustomerOtpEmailTemplate.BuildPlainTextBody(message.Otp, expiresAt),
                Encoding.UTF8,
                "text/plain"));
        var htmlView = AlternateView.CreateAlternateViewFromString(
            CustomerOtpEmailTemplate.BuildBody(message.Otp, expiresAt),
            Encoding.UTF8,
            MediaTypeNames.Text.Html);
        if (File.Exists(LogoPath))
        {
            htmlView.LinkedResources.Add(new LinkedResource(
                LogoPath,
                MediaTypeNames.Image.Png)
            {
                ContentId = CustomerOtpEmailTemplate.LogoContentId,
                TransferEncoding = TransferEncoding.Base64
            });
        }

        mail.AlternateViews.Add(htmlView);
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
}
