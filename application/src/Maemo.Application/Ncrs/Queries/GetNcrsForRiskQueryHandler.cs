using Maemo.Application.Common;
using Maemo.Application.Ncrs.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Ncrs.Queries;

public class GetNcrsForRiskQueryHandler : IRequestHandler<GetNcrsForRiskQuery, IReadOnlyList<NcrDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetNcrsForRiskQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<NcrDto>> Handle(GetNcrsForRiskQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify Risk exists and belongs to tenant
        var riskExists = await _context.Risks
            .AnyAsync(r => r.Id == request.RiskId && r.TenantId == tenantId, cancellationToken);

        if (!riskExists)
        {
            return Array.Empty<NcrDto>();
        }

        // Get all NCRs linked to this Risk
        var ncrs = await _context.NcrRiskLinks
            .Where(l => l.RiskId == request.RiskId && l.TenantId == tenantId)
            .Join(
                _context.Ncrs.Where(n => n.TenantId == tenantId),
                link => link.NcrId,
                ncr => ncr.Id,
                (link, ncr) => ncr)
            .Select(n => new NcrDto
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                Department = n.Department,
                OwnerUserId = n.OwnerUserId,
                Severity = n.Severity,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                DueDate = n.DueDate,
                ClosedAt = n.ClosedAt,
                Category = n.Category,
                RootCause = n.RootCause,
                CorrectiveAction = n.CorrectiveAction,
                EscalationLevel = n.EscalationLevel
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return ncrs;
    }
}

