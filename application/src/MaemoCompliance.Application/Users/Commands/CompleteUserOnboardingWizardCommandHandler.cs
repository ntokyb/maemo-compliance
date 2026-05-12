using System.Text.Json;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MaemoCompliance.Application.Users.Commands;

public class CompleteUserOnboardingWizardCommandHandler : IRequestHandler<CompleteUserOnboardingWizardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public CompleteUserOnboardingWizardCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUser,
        IMediator mediator,
        IConfiguration configuration)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _mediator = mediator;
        _configuration = configuration;
    }

    public async Task Handle(CompleteUserOnboardingWizardCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant())
        {
            throw new InvalidOperationException("Tenant context required.");
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        var email = _currentUser.UserEmail
            ?? throw new InvalidOperationException("User email required.");

        var emailNorm = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.Email.ToLower() == emailNorm,
                cancellationToken)
            ?? throw new KeyNotFoundException("User not found in workspace.");

        var fullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim();
        if (!string.IsNullOrEmpty(fullName))
        {
            user.FullName = fullName;
        }

        user.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.AddressLine = string.IsNullOrWhiteSpace(request.OrganisationAddress)
            ? null
            : request.OrganisationAddress.Trim();
        user.ComplianceStandardsJson = JsonSerializer.Serialize(request.Standards ?? Array.Empty<string>());
        user.OnboardingComplete = true;

        await _context.SaveChangesAsync(cancellationToken);

        var hasOrgBranding =
            !string.IsNullOrWhiteSpace(request.OrganisationName) || !string.IsNullOrWhiteSpace(request.LogoUrl);
        if (hasOrgBranding)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
            if (tenant != null)
            {
                var name = string.IsNullOrWhiteSpace(request.OrganisationName)
                    ? tenant.Name
                    : request.OrganisationName.Trim();
                try
                {
                    await _mediator.Send(
                        new UpdateTenantPortalGeneralCommand(
                            name,
                            string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
                            null),
                        cancellationToken);
                }
                catch (ArgumentException)
                {
                    // Invalid name/logo — user onboarding still saved
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        var portalBase = _configuration["App:PublicPortalUrl"] ?? "";
        var invited = 0;
        foreach (var raw in request.TeamEmails ?? Array.Empty<string>())
        {
            if (invited >= 5)
            {
                break;
            }

            var em = raw.Trim();
            if (string.IsNullOrWhiteSpace(em) || em.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                await _mediator.Send(new InviteUserCommand(em, "User", portalBase), cancellationToken);
                invited++;
            }
            catch (InvalidOperationException)
            {
                // Duplicate invite or seat limit — skip
            }
            catch (ArgumentException)
            {
                // Invalid email / role — skip
            }
        }
    }
}
