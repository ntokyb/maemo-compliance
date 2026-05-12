using Maemo.Application.Common;
using Maemo.Application.Risks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Risks.Commands;

public class UpdateRiskCommandHandler : IRequestHandler<UpdateRiskCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateRiskCommandHandler(
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

    public async Task Handle(UpdateRiskCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        var risk = await _context.Risks
            .Where(r => r.Id == request.Id && r.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (risk == null)
        {
            throw new InvalidOperationException($"Risk with ID {request.Id} not found.");
        }

        // Compute scores
        var inherentScore = request.Request.InherentLikelihood * request.Request.InherentImpact;
        var residualScore = request.Request.ResidualLikelihood * request.Request.ResidualImpact;

        risk.Title = request.Request.Title;
        risk.Description = request.Request.Description;
        risk.Category = request.Request.Category;
        risk.Cause = request.Request.Cause;
        risk.Consequences = request.Request.Consequences;
        risk.InherentLikelihood = request.Request.InherentLikelihood;
        risk.InherentImpact = request.Request.InherentImpact;
        risk.InherentScore = inherentScore;
        risk.ExistingControls = request.Request.ExistingControls;
        risk.ResidualLikelihood = request.Request.ResidualLikelihood;
        risk.ResidualImpact = request.Request.ResidualImpact;
        risk.ResidualScore = residualScore;
        risk.OwnerUserId = request.Request.OwnerUserId;
        risk.Status = request.Request.Status;
        risk.ModifiedAt = _dateTimeProvider.UtcNow;
        risk.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "UpdateRisk",
            "Risk",
            risk.Id,
            new { Title = risk.Title, Category = risk.Category, ResidualScore = risk.ResidualScore, Status = risk.Status.ToString() },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Risk.Updated",
            "Risk",
            risk.Id.ToString(),
            new { Title = risk.Title, Category = risk.Category, ResidualScore = risk.ResidualScore, Status = risk.Status.ToString() },
            cancellationToken);
    }
}

