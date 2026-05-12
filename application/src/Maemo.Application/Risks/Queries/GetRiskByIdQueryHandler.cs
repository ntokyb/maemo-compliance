using Maemo.Application.Common;
using Maemo.Application.Risks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Risks.Queries;

public class GetRiskByIdQueryHandler : IRequestHandler<GetRiskByIdQuery, RiskDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetRiskByIdQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<RiskDto?> Handle(GetRiskByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        return await _context.Risks
            .Where(r => r.Id == request.Id && r.TenantId == tenantId)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}

