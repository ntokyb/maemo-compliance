using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditRunsQueryHandler : IRequestHandler<GetAuditRunsQuery, IReadOnlyList<AuditRunDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetAuditRunsQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<AuditRunDto>> Handle(GetAuditRunsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Get all audit runs for current tenant
        var auditRuns = await _context.AuditRuns
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => new AuditRunDto
            {
                Id = r.Id,
                TenantId = r.TenantId,
                AuditTemplateId = r.AuditTemplateId,
                TemplateName = r.AuditTemplateId != Guid.Empty ? 
                    _context.AuditTemplates
                        .Where(t => t.Id == r.AuditTemplateId)
                        .Select(t => t.Name)
                        .FirstOrDefault() : null,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                AuditorUserId = r.AuditorUserId
            })
            .ToListAsync(cancellationToken);

        return auditRuns;
    }
}

