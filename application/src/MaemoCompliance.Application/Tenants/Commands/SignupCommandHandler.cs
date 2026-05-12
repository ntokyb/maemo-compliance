using System.Net.Mail;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Mail;
using MaemoCompliance.Domain.Users;
using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Commands;

public class SignupCommandHandler : IRequestHandler<SignupCommand, SignupResultDto>
{
    private static readonly HashSet<string> AllowedPlans =
    [
        "Starter", "Professional"
    ];

    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly IFeatureFlags _featureFlags;
    private readonly IPublicSignupRateLimiter _rateLimiter;
    private readonly IEmailSender _emailSender;
    private readonly IBusinessAuditLogger _auditLogger;
    private readonly IDateTimeProvider _clock;

    public SignupCommandHandler(
        IApplicationDbContext context,
        IMediator mediator,
        IFeatureFlags featureFlags,
        IPublicSignupRateLimiter rateLimiter,
        IEmailSender emailSender,
        IBusinessAuditLogger auditLogger,
        IDateTimeProvider clock)
    {
        _context = context;
        _mediator = mediator;
        _featureFlags = featureFlags;
        _rateLimiter = rateLimiter;
        _emailSender = emailSender;
        _auditLogger = auditLogger;
        _clock = clock;
    }

    public async Task<SignupResultDto> Handle(SignupCommand request, CancellationToken cancellationToken)
    {
        if (!_featureFlags.SelfServiceSignupEnabled)
        {
            throw new InvalidOperationException("Self-service signup is disabled.");
        }

        var ipKey = string.IsNullOrWhiteSpace(request.ClientIp) ? "unknown" : request.ClientIp.Trim();
        if (!_rateLimiter.TryAllow(ipKey))
        {
            throw new InvalidOperationException("Too many signup attempts from this network. Try again in an hour.");
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new ArgumentException("Company name is required.");
        }

        if (!IsValidEmail(request.AdminEmail))
        {
            throw new ArgumentException("A valid work email is required.");
        }

        var plan = request.Plan.Trim();
        if (!AllowedPlans.Contains(plan, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Plan must be Starter or Professional.");
        }

        var emailNorm = request.AdminEmail.Trim().ToLowerInvariant();

        var tenantEmailTaken = await _context.Tenants
            .AnyAsync(t => t.AdminEmail.ToLower() == emailNorm, cancellationToken);

        if (tenantEmailTaken)
        {
            throw new InvalidOperationException("DUPLICATE_EMAIL");
        }

        var userEmailTaken = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == emailNorm, cancellationToken);

        if (userEmailTaken)
        {
            throw new InvalidOperationException("DUPLICATE_EMAIL");
        }

        DateTime? trialEnds = null;
        if (string.Equals(plan, "Starter", StringComparison.OrdinalIgnoreCase))
        {
            trialEnds = _clock.UtcNow.AddDays(14);
        }

        var iso = (request.IsoFrameworks ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var industry = string.IsNullOrWhiteSpace(request.Industry) ? "Other" : request.Industry.Trim();

        var tenantId = await _mediator.Send(new ProvisionTenantCommand
        {
            Name = request.CompanyName.Trim(),
            AdminEmail = request.AdminEmail.Trim(),
            Plan = plan,
            TrialEndsAt = trialEnds,
            Industry = industry,
            IsoFrameworks = iso,
            EnableDefaultComplianceModules = true
        }, cancellationToken);

        var displayName = $"{request.AdminFirstName.Trim()} {request.AdminLastName.Trim()}".Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = request.AdminEmail.Trim();
        }

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = request.AdminEmail.Trim(),
            FullName = displayName,
            Role = UserRole.TenantAdmin,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            CreatedBy = "Signup"
        };
        _context.Users.Add(adminUser);

        var welcomeBody =
            $"Welcome to Maemo, {request.CompanyName}.\n\n" +
            "Connect Microsoft 365 under Tenant settings to store documents in SharePoint.\n" +
            "Sign in with your work account using the Maemo portal login.";

        var welcome = new WelcomeEmail
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ToEmail = request.AdminEmail.Trim(),
            Subject = "Your Maemo workspace is ready",
            Body = welcomeBody,
            CreatedAt = _clock.UtcNow,
            CreatedBy = "Signup"
        };
        _context.WelcomeEmails.Add(welcome);
        await _context.SaveChangesAsync(cancellationToken);

        await _emailSender.SendAsync(
            welcome.ToEmail,
            welcome.Subject,
            welcome.Body,
            cancellationToken);

        welcome.SentAt = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogForTenantAsync(
            tenantId,
            "Tenant.SelfServiceSignup",
            "Tenant",
            tenantId.ToString(),
            new { request.CompanyName, request.AdminEmail, plan, industry, iso },
            cancellationToken);

        return new SignupResultDto
        {
            TenantId = tenantId,
            Message = "Account created. Check your email.",
            NextStep = "Connect your Microsoft 365 account at /admin/m365-connection or Tenant settings."
        };
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
