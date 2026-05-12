using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Audits.Queries;
using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Commands;

public class CompleteAuditRunCommandHandler : IRequestHandler<CompleteAuditRunCommand, AuditRunDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBusinessAuditLogger _businessAuditLogger;
    private readonly IMediator _mediator;

    public CompleteAuditRunCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IBusinessAuditLogger businessAuditLogger,
        IMediator mediator)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _businessAuditLogger = businessAuditLogger;
        _mediator = mediator;
    }

    public async Task<AuditRunDto> Handle(CompleteAuditRunCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var run = await _context.AuditRuns
            .FirstOrDefaultAsync(r => r.Id == request.AuditRunId && r.TenantId == tenantId, cancellationToken);

        if (run == null)
        {
            throw new KeyNotFoundException($"Audit run with Id {request.AuditRunId} was not found for current tenant.");
        }

        if (run.CompletedAt.HasValue)
        {
            throw new ConflictException("This audit run is already completed.");
        }

        run.CompletedAt = _dateTimeProvider.UtcNow;
        run.ModifiedAt = _dateTimeProvider.UtcNow;
        run.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _businessAuditLogger.LogAsync(
            "AuditRun.Completed",
            "AuditRun",
            run.Id.ToString(),
            new { CompletedAt = run.CompletedAt, CompletedBy = _currentUserService.UserId },
            cancellationToken);

        return await _mediator.Send(new GetAuditRunByIdQuery { Id = run.Id }, cancellationToken);
    }
}
