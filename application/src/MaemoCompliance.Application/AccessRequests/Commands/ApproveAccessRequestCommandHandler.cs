using System.Text.Json;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Commands;
using MaemoCompliance.Domain.AccessRequests;
using MaemoCompliance.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MaemoCompliance.Application.AccessRequests.Commands;

public class ApproveAccessRequestCommandHandler : IRequestHandler<ApproveAccessRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ApproveAccessRequestCommandHandler(
        IApplicationDbContext context,
        IMediator mediator,
        IEmailSender emailSender,
        IConfiguration configuration,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _mediator = mediator;
        _emailSender = emailSender;
        _configuration = configuration;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(ApproveAccessRequestCommand request, CancellationToken cancellationToken)
    {
        var ar = await _context.AccessRequests
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Access request not found.");

        if (ar.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException("Request is not pending.");
        }

        var provisionPlan = MapPlan(request.Plan);
        if (provisionPlan == null)
        {
            throw new ArgumentException("Plan must be Starter, Growth, or Enterprise.");
        }

        var emailNorm = ar.ContactEmail.Trim().ToLowerInvariant();
        if (await _context.Tenants.AnyAsync(t => t.AdminEmail.ToLower() == emailNorm, cancellationToken)
            || await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNorm, cancellationToken))
        {
            throw new InvalidOperationException("This email is already registered.");
        }

        var standards = TryParseStandards(ar.TargetStandardsJson);

        DateTime? trialEnds = string.Equals(provisionPlan, "Starter", StringComparison.OrdinalIgnoreCase)
            ? _clock.UtcNow.AddDays(14)
            : null;

        var tenantId = await _mediator.Send(
            new ProvisionTenantCommand
            {
                Name = request.CompanyName.Trim(),
                AdminEmail = ar.ContactEmail.Trim(),
                Plan = provisionPlan,
                TrialEndsAt = trialEnds,
                Industry = ar.Industry,
                IsoFrameworks = standards,
                EnableDefaultComplianceModules = true
            },
            cancellationToken);

        var tenant = await _context.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        var now = _clock.UtcNow;

        var invite = new UserInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = ar.ContactEmail.Trim(),
            Role = UserRole.TenantAdmin,
            Token = token,
            ExpiresAt = now.AddHours(72),
            InvitedByUserId = _currentUser.UserId ?? "PlatformAdmin",
            CreatedAt = now,
            CreatedBy = _currentUser.UserId ?? "PlatformAdmin"
        };
        _context.UserInvitations.Add(invite);

        ar.Status = AccessRequestStatus.Approved;
        ar.ReviewedAt = now;
        ar.ReviewedBy = _currentUser.UserEmail ?? _currentUser.UserId;
        ar.CreatedTenantId = tenantId;

        await _context.SaveChangesAsync(cancellationToken);

        var baseUrl = (_configuration["App:PublicPortalUrl"] ?? "").TrimEnd('/');
        var link = string.IsNullOrEmpty(baseUrl)
            ? $"/accept-invite?token={token}"
            : $"{baseUrl}/accept-invite?token={token}";

        var body =
            $"You have been invited to join {tenant.Name} on Maemo Compliance.\n\n" +
            $"Accept your invitation and sign in with Microsoft: {link}\n";

        await _emailSender.SendAsync(
            ar.ContactEmail.Trim(),
            $"You're invited to Maemo Compliance — {tenant.Name}",
            body,
            cancellationToken);
    }

    private static string? MapPlan(string plan)
    {
        var p = plan.Trim();
        if (string.Equals(p, "Starter", StringComparison.OrdinalIgnoreCase))
        {
            return "Starter";
        }

        if (string.Equals(p, "Growth", StringComparison.OrdinalIgnoreCase))
        {
            return "Professional";
        }

        if (string.Equals(p, "Enterprise", StringComparison.OrdinalIgnoreCase))
        {
            return "Enterprise";
        }

        return null;
    }

    private static List<string> TryParseStandards(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
