using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Domain.Risks;
using MediatR;

namespace MaemoCompliance.Application.Risks.Commands;

public class CreateRiskCommandHandler : IRequestHandler<CreateRiskCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public CreateRiskCommandHandler(
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

    public async Task<Guid> Handle(CreateRiskCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        
        // Compute scores
        var inherentScore = request.Request.InherentLikelihood * request.Request.InherentImpact;
        var residualScore = request.Request.ResidualLikelihood * request.Request.ResidualImpact;

        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Title = request.Request.Title,
            Description = request.Request.Description,
            Category = request.Request.Category,
            Cause = request.Request.Cause,
            Consequences = request.Request.Consequences,
            InherentLikelihood = request.Request.InherentLikelihood,
            InherentImpact = request.Request.InherentImpact,
            InherentScore = inherentScore,
            ExistingControls = request.Request.ExistingControls,
            ResidualLikelihood = request.Request.ResidualLikelihood,
            ResidualImpact = request.Request.ResidualImpact,
            ResidualScore = residualScore,
            OwnerUserId = request.Request.OwnerUserId,
            Status = request.Request.Status,
            CreatedAt = now,
            CreatedBy = _currentUserService.UserId
        };

        _context.Risks.Add(risk);
        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "CreateRisk",
            "Risk",
            risk.Id,
            new { Title = risk.Title, Category = risk.Category, ResidualScore = risk.ResidualScore, Status = risk.Status.ToString() },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Risk.Created",
            "Risk",
            risk.Id.ToString(),
            new { Title = risk.Title, Category = risk.Category, ResidualScore = risk.ResidualScore, Status = risk.Status.ToString() },
            cancellationToken);

        return risk.Id;
    }
}

