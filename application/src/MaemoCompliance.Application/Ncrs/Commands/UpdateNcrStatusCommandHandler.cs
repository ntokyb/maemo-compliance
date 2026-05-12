using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class UpdateNcrStatusCommandHandler : IRequestHandler<UpdateNcrStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateNcrStatusCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(UpdateNcrStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var ncr = await _context.Ncrs
            .Where(n => n.Id == request.Id && n.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ncr == null)
        {
            throw new KeyNotFoundException($"NCR with Id {request.Id} not found for tenant {tenantId}");
        }

        var previousStatus = ncr.Status;
        
        // Create history record before changing status (if status is actually changing)
        if (previousStatus != request.Status)
        {
            var history = new NcrStatusHistory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                NcrId = ncr.Id,
                OldStatus = previousStatus,
                NewStatus = request.Status,
                ChangedAt = _dateTimeProvider.UtcNow,
                ChangedByUserId = _currentUserService.UserId,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.NcrStatusHistory.Add(history);
        }

        ncr.Status = request.Status;

        // If status changes to Closed, set ClosedAt
        if (request.Status == NcrStatus.Closed && previousStatus != NcrStatus.Closed)
        {
            ncr.ClosedAt = request.ClosedAt ?? _dateTimeProvider.UtcNow;
        }
        else if (request.Status != NcrStatus.Closed)
        {
            // If status changes from Closed to something else, clear ClosedAt
            ncr.ClosedAt = null;
        }
        else if (request.ClosedAt.HasValue)
        {
            // If already closed but ClosedAt is being updated
            ncr.ClosedAt = request.ClosedAt.Value;
        }

        // Update DueDate if provided
        if (request.DueDate.HasValue)
        {
            ncr.DueDate = request.DueDate.Value;
        }

        ncr.ModifiedAt = _dateTimeProvider.UtcNow;
        ncr.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "UpdateNcrStatus",
            "Ncr",
            ncr.Id,
            new { PreviousStatus = previousStatus.ToString(), NewStatus = request.Status.ToString(), ClosedAt = ncr.ClosedAt },
            cancellationToken);

        // Business audit log (only if status actually changed)
        if (previousStatus != request.Status)
        {
            await _businessAuditLogger.LogAsync(
                "NCR.StatusChanged",
                "NCR",
                ncr.Id.ToString(),
                new { OldStatus = previousStatus.ToString(), NewStatus = request.Status.ToString(), ClosedAt = ncr.ClosedAt },
                cancellationToken);
        }
    }
}

