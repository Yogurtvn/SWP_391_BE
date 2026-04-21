using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Email;

namespace ServiceLayer.Services.Email;

public class SmtpEmailService(IOptions<EmailSettings> emailOptions) : IEmailService
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;

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
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (string.IsNullOrWhiteSpace(_emailSettings.Username))
        {
            smtpClient.UseDefaultCredentials = true;
        }
        else
        {
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(
                _emailSettings.Username.Trim(),
                _emailSettings.Password);
        }

        await smtpClient.SendMailAsync(message, cancellationToken);
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

        if (string.IsNullOrWhiteSpace(_emailSettings.Username) && string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
        {
            throw new InvalidOperationException(
                "SMTP sender is not configured. Set EmailSettings:FromEmail or EmailSettings:Username.");
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
