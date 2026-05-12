using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class UnlinkNcrFromRiskCommandHandler : IRequestHandler<UnlinkNcrFromRiskCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public UnlinkNcrFromRiskCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task Handle(UnlinkNcrFromRiskCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var link = await _context.NcrRiskLinks
            .FirstOrDefaultAsync(
                l => l.NcrId == request.NcrId && 
                     l.RiskId == request.RiskId && 
                     l.TenantId == tenantId,
                cancellationToken);

        if (link != null)
        {
            _context.NcrRiskLinks.Remove(link);
            await _context.SaveChangesAsync(cancellationToken);
        }
        // If link doesn't exist, silently succeed (idempotent operation)
    }
}

