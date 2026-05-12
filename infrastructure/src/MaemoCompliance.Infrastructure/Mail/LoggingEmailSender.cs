using MaemoCompliance.Application.Common;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Infrastructure.Mail;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] To={To} Subject={Subject} BodyLength={Length}",
            toEmail,
            subject,
            body?.Length ?? 0);
        return Task.CompletedTask;
    }
}
