namespace MaemoCompliance.Application.Common;

/// <summary>
/// Sends transactional email. Configure <c>Resend:ApiKey</c> and <c>Resend:FromAddress</c> for production (Resend HTTP API);
/// when the API key is absent, the infrastructure layer uses a logging-only implementation.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
