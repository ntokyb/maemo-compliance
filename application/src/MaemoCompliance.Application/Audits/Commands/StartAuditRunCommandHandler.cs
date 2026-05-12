using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Audits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Commands;

public class StartAuditRunCommandHandler : IRequestHandler<StartAuditRunCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public StartAuditRunCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task<Guid> Handle(StartAuditRunCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify template exists
        var template = await _context.AuditTemplates
            .FirstOrDefaultAsync(t => t.Id == command.AuditTemplateId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Audit template with Id {command.AuditTemplateId} not found.");
        }

        var auditRun = new AuditRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuditTemplateId = command.AuditTemplateId,
            StartedAt = _dateTimeProvider.UtcNow,
            AuditorUserId = command.AuditorUserId ?? _currentUserService.UserId,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.UserId
        };

        _context.AuditRuns.Add(auditRun);
        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "AuditRun.Started",
            "AuditRun",
            auditRun.Id.ToString(),
            new { AuditTemplateId = command.AuditTemplateId, AuditorUserId = auditRun.AuditorUserId },
            cancellationToken);

        return auditRun.Id;
    }
}

