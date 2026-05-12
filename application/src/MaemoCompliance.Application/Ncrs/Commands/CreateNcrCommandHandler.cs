using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class CreateNcrCommandHandler : IRequestHandler<CreateNcrCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public CreateNcrCommandHandler(
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

    public async Task<Guid> Handle(CreateNcrCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var ncr = new Ncr
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Title = request.Title,
            Description = request.Description,
            Department = request.Department,
            OwnerUserId = request.OwnerUserId,
            Severity = request.Severity,
            Status = NcrStatus.Open,
            CreatedAt = now,
            DueDate = request.DueDate,
            CreatedBy = _currentUserService.UserId,
            Category = request.Category,
            RootCause = request.RootCause,
            CorrectiveAction = request.CorrectiveAction,
            EscalationLevel = request.EscalationLevel
        };

        _context.Ncrs.Add(ncr);
        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "CreateNcr",
            "Ncr",
            ncr.Id,
            new { Title = ncr.Title, Severity = ncr.Severity.ToString(), Status = ncr.Status.ToString() },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "NCR.Created",
            "NCR",
            ncr.Id.ToString(),
            new { Title = ncr.Title, Severity = ncr.Severity.ToString(), Status = ncr.Status.ToString() },
            cancellationToken);

        return ncr.Id;
    }
}

