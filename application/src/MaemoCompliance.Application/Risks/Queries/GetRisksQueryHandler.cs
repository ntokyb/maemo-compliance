using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Domain.Risks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Risks.Queries;

public class GetRisksQueryHandler : IRequestHandler<GetRisksQuery, IReadOnlyList<RiskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetRisksQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<RiskDto>> Handle(GetRisksQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var query = _context.Risks
            .Where(r => r.TenantId == tenantId);

        if (request.Category.HasValue)
        {
            query = query.Where(r => r.Category == request.Category.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        return await query
            .OrderBy(r => r.Title)
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
    }
}

