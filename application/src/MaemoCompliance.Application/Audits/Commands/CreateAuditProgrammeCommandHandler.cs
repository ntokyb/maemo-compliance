using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Audits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Commands;

public sealed class CreateAuditProgrammeCommandHandler : IRequestHandler<CreateAuditProgrammeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public CreateAuditProgrammeCommandHandler(
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

    public async Task<Guid> Handle(CreateAuditProgrammeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var year = request.Year;

        var exists = await _context.AuditProgrammes
            .AnyAsync(p => p.TenantId == tenantId && p.Year == year, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"An audit programme for {year} already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var programme = new AuditProgramme
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Title = request.Title.Trim(),
            Status = AuditProgrammeStatus.Draft,
            CreatedAt = now,
            CreatedBy = _currentUserService.UserId,
        };

        foreach (var item in request.Items)
        {
            programme.Items.Add(new AuditScheduleItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AuditProgrammeId = programme.Id,
                ProcessArea = item.ProcessArea.Trim(),
                AuditorName = item.AuditorName.Trim(),
                PlannedDate = item.PlannedDate,
                Status = AuditScheduleItemStatus.Planned,
                CreatedAt = now,
                CreatedBy = _currentUserService.UserId,
            });
        }

        _context.AuditProgrammes.Add(programme);
        await _context.SaveChangesAsync(cancellationToken);

        return programme.Id;
    }
}
