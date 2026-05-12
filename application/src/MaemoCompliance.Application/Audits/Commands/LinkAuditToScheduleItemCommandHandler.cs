using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Audits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Commands;

public sealed class LinkAuditToScheduleItemCommandHandler : IRequestHandler<LinkAuditToScheduleItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public LinkAuditToScheduleItemCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task Handle(LinkAuditToScheduleItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var item = await _context.AuditScheduleItems
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId && i.AuditProgrammeId == request.ProgrammeId && i.TenantId == tenantId,
                cancellationToken);

        if (item == null)
        {
            throw new KeyNotFoundException("Schedule item was not found.");
        }

        var auditExists = await _context.AuditRuns
            .AnyAsync(r => r.Id == request.AuditId && r.TenantId == tenantId, cancellationToken);

        if (!auditExists)
        {
            throw new KeyNotFoundException("Audit run was not found.");
        }

        item.LinkedAuditId = request.AuditId;
        item.Status = AuditScheduleItemStatus.Complete;
        item.ModifiedAt = _dateTimeProvider.UtcNow;
        item.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
