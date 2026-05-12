using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class DeleteNcrCommandHandler : IRequestHandler<DeleteNcrCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public DeleteNcrCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(DeleteNcrCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var ncr = await _context.Ncrs
            .FirstOrDefaultAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (ncr == null)
        {
            throw new KeyNotFoundException($"NCR with Id {request.NcrId} not found for tenant {tenantId}");
        }

        if (ncr.Status == NcrStatus.Closed)
        {
            throw new ConflictException("A closed NCR cannot be deleted.");
        }

        var riskLinks = await _context.NcrRiskLinks
            .Where(l => l.NcrId == request.NcrId && l.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var history = await _context.NcrStatusHistory
            .Where(h => h.NcrId == request.NcrId && h.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        _context.NcrRiskLinks.RemoveRange(riskLinks);
        _context.NcrStatusHistory.RemoveRange(history);

        await _businessAuditLogger.LogAsync(
            "NCR.Deleted",
            "NCR",
            request.NcrId.ToString(),
            new { ncr.Title, Status = ncr.Status.ToString(), RiskLinkCount = riskLinks.Count, HistoryCount = history.Count },
            cancellationToken);

        _context.Ncrs.Remove(ncr);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
