namespace Maemo.Application.Common;

/// <summary>
/// Sends transactional email. Production: replace stub with SMTP/SendGrid/etc.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
