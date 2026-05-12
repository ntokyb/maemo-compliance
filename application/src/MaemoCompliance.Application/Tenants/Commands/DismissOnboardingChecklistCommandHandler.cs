using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Commands;

public class DismissOnboardingChecklistCommandHandler : IRequestHandler<DismissOnboardingChecklistCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public DismissOnboardingChecklistCommandHandler(IApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task Handle(DismissOnboardingChecklistCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} not found.");
        }

        var state = OnboardingChecklistState.FromTenant(tenant);
        state.Dismissed = true;
        OnboardingChecklistState.ApplyToTenant(tenant, state);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
