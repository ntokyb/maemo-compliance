using Maemo.Application.Common;
using Maemo.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Queries;

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetOnboardingStatusQueryHandler(IApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} not found.");
        }

        var state = OnboardingChecklistState.FromTenant(tenant);

        var step1 = !string.IsNullOrWhiteSpace(tenant.SharePointSiteUrl);
        var step2 = await _context.Documents.AnyAsync(d => d.TenantId == tenantId, cancellationToken);
        var step3 = await _context.Ncrs.AnyAsync(n => n.TenantId == tenantId, cancellationToken);
        var step4 = await _context.Risks.AnyAsync(r => r.TenantId == tenantId, cancellationToken);
        var step5 = await _context.AuditRuns.AnyAsync(a => a.TenantId == tenantId, cancellationToken);
        var memberCount = await _context.Users.CountAsync(
            u => u.TenantId == tenantId && u.IsActive,
            cancellationToken);
        var step6 = memberCount > 1;

        var flags = new[] { step1, step2, step3, step4, step5, step6 };
        var completed = flags.Count(f => f);

        var steps = new List<OnboardingStepStatusDto>
        {
            new()
            {
                Id = 1,
                Label = "Connect Microsoft 365 (SharePoint)",
                Complete = step1,
                Link = "/admin/tenant-settings?tab=sharepoint"
            },
            new()
            {
                Id = 2,
                Label = "Upload your first document",
                Complete = step2,
                Link = "/documents/new"
            },
            new()
            {
                Id = 3,
                Label = "Create your first NCR",
                Complete = step3,
                Link = "/ncrs/new"
            },
            new()
            {
                Id = 4,
                Label = "Set up a risk register entry",
                Complete = step4,
                Link = "/risks/new"
            },
            new()
            {
                Id = 5,
                Label = "Run your first audit",
                Complete = step5,
                Link = "/audits/templates/new"
            },
            new()
            {
                Id = 6,
                Label = "Invite a team member",
                Complete = step6,
                Link = "/admin/tenant-settings?tab=users"
            }
        };

        return new OnboardingStatusDto
        {
            Steps = steps,
            CompletedCount = completed,
            TotalCount = 6,
            AllComplete = completed >= 6,
            Dismissed = state.Dismissed
        };
    }
}
