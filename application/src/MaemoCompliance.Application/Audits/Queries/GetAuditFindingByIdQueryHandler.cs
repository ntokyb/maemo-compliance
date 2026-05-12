using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Queries;

public sealed class GetAuditFindingByIdQueryHandler : IRequestHandler<GetAuditFindingByIdQuery, AuditFindingDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetAuditFindingByIdQueryHandler(IApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<AuditFindingDto?> Handle(GetAuditFindingByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        return await _context.AuditFindings
            .Where(f => f.Id == request.FindingId && f.AuditRunId == request.AuditRunId && f.TenantId == tenantId)
            .Select(f => new AuditFindingDto
            {
                Id = f.Id,
                AuditRunId = f.AuditRunId,
                Title = f.Title,
                LinkedNcrId = f.LinkedNcrId,
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
