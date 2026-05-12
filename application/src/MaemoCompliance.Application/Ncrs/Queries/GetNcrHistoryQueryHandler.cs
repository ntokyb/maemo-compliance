using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrHistoryQueryHandler : IRequestHandler<GetNcrHistoryQuery, IReadOnlyList<NcrStatusHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetNcrHistoryQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<NcrStatusHistoryDto>> Handle(GetNcrHistoryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify the NCR exists and belongs to the tenant
        var ncrExists = await _context.Ncrs
            .AnyAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (!ncrExists)
        {
            return Array.Empty<NcrStatusHistoryDto>();
        }

        var history = await _context.NcrStatusHistory
            .Where(h => h.NcrId == request.NcrId && h.TenantId == tenantId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new NcrStatusHistoryDto
            {
                Id = h.Id,
                NcrId = h.NcrId,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                ChangedAt = h.ChangedAt,
                ChangedByUserId = h.ChangedByUserId
            })
            .ToListAsync(cancellationToken);

        return history;
    }
}

