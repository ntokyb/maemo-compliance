using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Application.Public.Commands;

public class SubmitPublicContactCommandHandler : IRequestHandler<SubmitPublicContactCommand>
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubmitPublicContactCommandHandler> _logger;

    public SubmitPublicContactCommandHandler(
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<SubmitPublicContactCommandHandler> logger)
    {
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(SubmitPublicContactCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Public contact: {Name} ({Email}) at {Company}",
            request.Name,
            request.Email,
            request.Company);

        var adminTo = _configuration["App:AdminNotificationEmail"] ?? "admin@maemo-compliance.co.za";
        var subject = $"Maemo Compliance contact — {request.Company}";
        var body =
            $"Name: {request.Name}\n" +
            $"Company: {request.Company}\n" +
            $"Email: {request.Email}\n\n" +
            $"Message:\n{request.Message}\n";

        await _emailSender.SendAsync(adminTo, subject, body, cancellationToken);
    }
}
