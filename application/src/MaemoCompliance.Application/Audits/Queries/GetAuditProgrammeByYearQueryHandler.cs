using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Queries;

public sealed class GetAuditProgrammeByYearQueryHandler : IRequestHandler<GetAuditProgrammeByYearQuery, AuditProgrammeDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetAuditProgrammeByYearQueryHandler(IApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<AuditProgrammeDto?> Handle(GetAuditProgrammeByYearQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var programme = await _context.AuditProgrammes
            .Include(p => p.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Year == request.Year, cancellationToken);

        if (programme == null)
        {
            return null;
        }

        return new AuditProgrammeDto
        {
            Id = programme.Id,
            Year = programme.Year,
            Title = programme.Title,
            Status = programme.Status,
            Items = programme.Items
                .OrderBy(i => i.PlannedDate)
                .Select(i => new AuditScheduleItemDto
                {
                    Id = i.Id,
                    ProcessArea = i.ProcessArea,
                    AuditorName = i.AuditorName,
                    PlannedDate = i.PlannedDate,
                    Status = i.Status,
                    LinkedAuditId = i.LinkedAuditId,
                })
                .ToList(),
        };
    }
}
