using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Onboarding;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Onboarding.Commands;

/// <summary>
/// Handler for running the onboarding wizard.
/// </summary>
public class RunOnboardingCommandHandler : IRequestHandler<RunOnboardingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOnboardingSeeder _onboardingSeeder;
    private readonly IAuditLogger _auditLogger;

    public RunOnboardingCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IOnboardingSeeder onboardingSeeder,
        IAuditLogger auditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _onboardingSeeder = onboardingSeeder;
        _auditLogger = auditLogger;
    }

    public async Task Handle(RunOnboardingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Get tenant
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
        }

        if (tenant.OnboardingCompleted)
        {
            throw new InvalidOperationException("Onboarding has already been completed for this tenant.");
        }

        // Seed data based on onboarding selections
        await _onboardingSeeder.SeedAsync(request.Request, cancellationToken);

        // Mark onboarding as completed
        tenant.OnboardingCompleted = true;
        tenant.OnboardingCompletedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedBy = _currentUserService.UserId ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "CompleteOnboarding",
            "Tenant",
            tenant.Id,
            new
            {
                IsoStandards = string.Join(", ", request.Request.IsoStandards),
                Industry = request.Request.Industry,
                CompanySize = request.Request.CompanySize
            },
            cancellationToken);
    }
}

