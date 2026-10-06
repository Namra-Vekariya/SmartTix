using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmartTix.Application.Configuration;
using SmartTix.Application.Interfaces.Services;

namespace SmartTix.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException(
                "Recipient email is required.",
                nameof(recipientEmail));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException(
                "Email subject is required.",
                nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException(
                "Email body is required.",
                nameof(body));

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _options.FromName,
                _options.FromAddress));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = isHtml ? body : null,
            TextBody = isHtml ? null : body
        }.ToMessageBody();

        try
        {
            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                GetSecureSocketOptions(_options.Port),
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {RecipientEmail}. Subject: {Subject}",
                recipientEmail,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {RecipientEmail}. Subject: {Subject}",
                recipientEmail,
                subject);

            throw;
        }
    }

    // 465 = implicit TLS, everything else (587/25) = STARTTLS
    private static SecureSocketOptions GetSecureSocketOptions(int port)
    {
        return port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}