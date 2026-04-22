using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Email;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ControllerLayer.Controllers;

[Route("api/email-diagnostics")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class EmailDiagnosticsController(IEmailService emailService, ILogger<EmailDiagnosticsController> logger) : ControllerBase
{
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<EmailDiagnosticsController> _logger = logger;

    [HttpPost("send-test")]
    public async Task<ActionResult> SendTestEmail([FromBody] SendTestEmailRequest request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmail?.Trim();

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            toEmail = User.FindFirstValue(ClaimTypes.Email)?.Trim();
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return BadRequest(new
            {
                errorCode = "MISSING_EMAIL",
                message = "ToEmail is required when the access token does not contain an email claim."
            });
        }

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? "SMTP test email"
            : request.Subject.Trim();
        var body = string.IsNullOrWhiteSpace(request.Body)
            ? "This is a test email from Online Eyewear API."
            : request.Body.Trim();

        try
        {
            await _emailService.SendEmailAsync(toEmail, subject, body, cancellationToken);
            return Ok(new
            {
                message = "Test email sent.",
                toEmail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send diagnostic email. RequestedByUserId: {RequestedByUserId}, ToEmail: {ToEmail}",
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                toEmail);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                errorCode = "EMAIL_SEND_FAILED",
                message = "Failed to send test email. Check SMTP configuration and server logs."
            });
        }
    }

    public class SendTestEmailRequest
    {
        [EmailAddress]
        public string? ToEmail { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }
    }
}
