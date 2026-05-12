using System.Net.Mail;
using System.Text.Json;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AccessRequests;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MaemoCompliance.Application.Public.Commands;

public class SubmitAccessRequestCommandHandler : IRequestHandler<SubmitAccessRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IPublicSignupRateLimiter _rateLimiter;
    private readonly IDateTimeProvider _clock;

    public SubmitAccessRequestCommandHandler(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IConfiguration configuration,
        IPublicSignupRateLimiter rateLimiter,
        IDateTimeProvider clock)
    {
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
        _rateLimiter = rateLimiter;
        _clock = clock;
    }

    public async Task<Guid> Handle(SubmitAccessRequestCommand request, CancellationToken cancellationToken)
    {
        var ipKey = string.IsNullOrWhiteSpace(request.ClientIp) ? "unknown" : request.ClientIp.Trim();
        if (!_rateLimiter.TryAllow(ipKey))
        {
            throw new InvalidOperationException("Too many requests from this network. Try again in an hour.");
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new ArgumentException("Company name is required.");
        }

        if (!IsValidEmail(request.ContactEmail))
        {
            throw new ArgumentException("A valid email is required.");
        }

        var emailNorm = request.ContactEmail.Trim().ToLowerInvariant();
        var pending = await _context.AccessRequests.AnyAsync(
            a => a.ContactEmail.ToLower() == emailNorm && a.Status == AccessRequestStatus.Pending,
            cancellationToken);

        if (pending)
        {
            throw new InvalidOperationException("DUPLICATE_PENDING_REQUEST");
        }

        var standards = (request.TargetStandards ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var entity = new AccessRequest
        {
            Id = Guid.NewGuid(),
            CompanyName = request.CompanyName.Trim(),
            Industry = request.Industry.Trim(),
            CompanySize = request.CompanySize.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            ContactRole = request.ContactRole.Trim(),
            TargetStandardsJson = JsonSerializer.Serialize(standards),
            ReferralSource = request.ReferralSource.Trim(),
            Status = AccessRequestStatus.Pending,
            CreatedAt = _clock.UtcNow,
            CreatedBy = "PublicRequestAccess"
        };

        _context.AccessRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var adminTo = _configuration["App:AdminNotificationEmail"] ?? "admin@maemo-compliance.co.za";
        var adminSubject = $"New access request — {entity.CompanyName}";
        var adminBody =
            $"Company: {entity.CompanyName}\n" +
            $"Industry: {entity.Industry}\n" +
            $"Size: {entity.CompanySize}\n" +
            $"Contact: {entity.ContactName} <{entity.ContactEmail}>\n" +
            $"Role: {entity.ContactRole}\n" +
            $"Standards: {string.Join(", ", standards)}\n" +
            $"Referral: {entity.ReferralSource}\n";

        await _emailSender.SendAsync(adminTo, adminSubject, adminBody, cancellationToken);

        var confirmBody =
            $"Hi {entity.ContactName},\n\n" +
            "Thank you for your interest in Maemo Compliance.\n" +
            "We review all requests within 1 business day.\n" +
            $"We will be in touch at {entity.ContactEmail}.\n";

        await _emailSender.SendAsync(
            entity.ContactEmail,
            "We received your request — Maemo Compliance",
            confirmBody,
            cancellationToken);

        return entity.Id;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }
}
