using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditRunByIdQueryHandler : IRequestHandler<GetAuditRunByIdQuery, AuditRunDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetAuditRunByIdQueryHandler(IApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<AuditRunDto> Handle(GetAuditRunByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var dto = await _context.AuditRuns
            .Where(r => r.Id == request.Id && r.TenantId == tenantId)
            .Select(r => new AuditRunDto
            {
                Id = r.Id,
                TenantId = r.TenantId,
                AuditTemplateId = r.AuditTemplateId,
                TemplateName = _context.AuditTemplates
                    .Where(t => t.Id == r.AuditTemplateId)
                    .Select(t => t.Name)
                    .FirstOrDefault(),
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                AuditorUserId = r.AuditorUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
        {
            throw new KeyNotFoundException($"Audit run with Id {request.Id} was not found for current tenant.");
        }

        return dto;
    }
}
