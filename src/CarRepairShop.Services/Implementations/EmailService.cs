using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Domain.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CarRepairShop.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly Func<ISmtpClient> _clientFactory;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        : this(smtpSettings, logger, () => new SmtpClient()) { }

    internal EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger, Func<ISmtpClient> clientFactory)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
        _clientFactory = clientFactory;

        if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
            throw new InvalidOperationException("SMTP Host is not configured.");
        if (string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            throw new InvalidOperationException("SMTP FromEmail is not configured.");
        if (string.IsNullOrWhiteSpace(_smtpSettings.Username))
            throw new InvalidOperationException("SMTP Username is not configured.");
        if (string.IsNullOrWhiteSpace(_smtpSettings.Password))
            throw new InvalidOperationException("SMTP Password is not configured.");
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
            bodyBuilder.HtmlBody = body;
        else
            bodyBuilder.TextBody = body;

        message.Body = bodyBuilder.ToMessageBody();

        using var client = _clientFactory();
        try
        {
            // Use SslOnConnect (direct TLS on port 465) to avoid STARTTLS vulnerabilities.
            // If UseSsl is false, connect without encryption (not recommended for production).
            var socketOptions = _smtpSettings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions, cancellationToken);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully with subject '{Subject}'.", subject);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
