using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Email;

namespace ServiceLayer.Services.Email;

public class SmtpEmailService(
    IOptions<EmailSettings> emailOptions,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;
    private readonly ILogger<SmtpEmailService> _logger = logger;

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        ValidateConfiguration();

        var fromEmail = ResolveFromEmail();
        var fromAddress = string.IsNullOrWhiteSpace(_emailSettings.FromName)
            ? new MailAddress(fromEmail)
            : new MailAddress(fromEmail, _emailSettings.FromName.Trim());

        using var message = new MailMessage
        {
            From = fromAddress,
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail.Trim());

        using var smtpClient = new SmtpClient(_emailSettings.Host.Trim(), _emailSettings.Port)
        {
            EnableSsl = _emailSettings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                _emailSettings.Username.Trim(),
                _emailSettings.Password)
        };

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);
            _logger.LogInformation(
                "Email sent successfully. To: {ToEmail}, Subject: {Subject}",
                toEmail.Trim(),
                subject.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email via SMTP. To: {ToEmail}, Host: {Host}, Port: {Port}, EnableSsl: {EnableSsl}",
                toEmail.Trim(),
                _emailSettings.Host.Trim(),
                _emailSettings.Port,
                _emailSettings.EnableSsl);
            throw;
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_emailSettings.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured. Set EmailSettings:Host.");
        }

        if (_emailSettings.Port <= 0)
        {
            throw new InvalidOperationException("SMTP port is invalid. Set EmailSettings:Port to a positive value.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Username))
        {
            throw new InvalidOperationException(
                "SMTP username is not configured. Set EmailSettings:Username.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Password))
        {
            throw new InvalidOperationException(
                "SMTP password is not configured. Set EmailSettings:Password.");
        }
    }

    private string ResolveFromEmail()
    {
        if (!string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
        {
            return _emailSettings.FromEmail.Trim();
        }

        return _emailSettings.Username.Trim();
    }
}
