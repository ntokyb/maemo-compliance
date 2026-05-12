using Maemo.Application.Common;
using Maemo.Application.Ncrs.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Ncrs.Commands;

public class UpdateNcrCommandHandler : IRequestHandler<UpdateNcrCommand, NcrDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateNcrCommandHandler(
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

    public async Task<NcrDto> Handle(UpdateNcrCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var ncr = await _context.Ncrs
            .FirstOrDefaultAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (ncr == null)
        {
            throw new KeyNotFoundException($"NCR with Id {request.NcrId} not found for tenant {tenantId}");
        }

        var req = request.Request;

        ncr.Title = req.Title;
        ncr.Description = req.Description;
        ncr.Department = req.Department;
        ncr.OwnerUserId = req.OwnerUserId;
        ncr.Severity = req.Severity;
        ncr.DueDate = req.DueDate;
        ncr.Category = req.Category;
        ncr.RootCause = req.RootCause;
        ncr.CorrectiveAction = req.CorrectiveAction;
        ncr.EscalationLevel = req.EscalationLevel;

        ncr.ModifiedAt = _dateTimeProvider.UtcNow;
        ncr.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "UpdateNcr",
            "Ncr",
            ncr.Id,
            new { ncr.Title, Severity = ncr.Severity.ToString(), Status = ncr.Status.ToString() },
            cancellationToken);

        await _businessAuditLogger.LogAsync(
            "NCR.Updated",
            "NCR",
            ncr.Id.ToString(),
            new { ncr.Title, Severity = ncr.Severity.ToString(), Status = ncr.Status.ToString() },
            cancellationToken);

        return new NcrDto
        {
            Id = ncr.Id,
            Title = ncr.Title,
            Description = ncr.Description,
            Department = ncr.Department,
            OwnerUserId = ncr.OwnerUserId,
            Severity = ncr.Severity,
            Status = ncr.Status,
            CreatedAt = ncr.CreatedAt,
            DueDate = ncr.DueDate,
            ClosedAt = ncr.ClosedAt,
            Category = ncr.Category,
            RootCause = ncr.RootCause,
            CorrectiveAction = ncr.CorrectiveAction,
            EscalationLevel = ncr.EscalationLevel
        };
    }
}
