using Maemo.Application.Common;
using Maemo.Application.Risks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Ncrs.Queries;

public class GetRisksForNcrQueryHandler : IRequestHandler<GetRisksForNcrQuery, IReadOnlyList<RiskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetRisksForNcrQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<RiskDto>> Handle(GetRisksForNcrQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify NCR exists and belongs to tenant
        var ncrExists = await _context.Ncrs
            .AnyAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (!ncrExists)
        {
            return Array.Empty<RiskDto>();
        }

        // Get all risks linked to this NCR
        var risks = await _context.NcrRiskLinks
            .Where(l => l.NcrId == request.NcrId && l.TenantId == tenantId)
            .Join(
                _context.Risks.Where(r => r.TenantId == tenantId),
                link => link.RiskId,
                risk => risk.Id,
                (link, risk) => risk)
            .Select(r => new RiskDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Category = r.Category,
                Cause = r.Cause,
                Consequences = r.Consequences,
                InherentLikelihood = r.InherentLikelihood,
                InherentImpact = r.InherentImpact,
                InherentScore = r.InherentScore,
                ExistingControls = r.ExistingControls,
                ResidualLikelihood = r.ResidualLikelihood,
                ResidualImpact = r.ResidualImpact,
                ResidualScore = r.ResidualScore,
                OwnerUserId = r.OwnerUserId,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ModifiedAt = r.ModifiedAt
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return risks;
    }
}

