using Maemo.Application.Common;
using Maemo.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Ncrs.Commands;

public class LinkNcrToRiskCommandHandler : IRequestHandler<LinkNcrToRiskCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public LinkNcrToRiskCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(LinkNcrToRiskCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Validate NCR exists and belongs to tenant
        var ncrExists = await _context.Ncrs
            .AnyAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (!ncrExists)
        {
            throw new InvalidOperationException($"NCR with ID {request.NcrId} not found.");
        }

        // Validate Risk exists and belongs to tenant
        var riskExists = await _context.Risks
            .AnyAsync(r => r.Id == request.RiskId && r.TenantId == tenantId, cancellationToken);

        if (!riskExists)
        {
            throw new InvalidOperationException($"Risk with ID {request.RiskId} not found.");
        }

        // Check if link already exists
        var existingLink = await _context.NcrRiskLinks
            .FirstOrDefaultAsync(
                l => l.NcrId == request.NcrId && 
                     l.RiskId == request.RiskId && 
                     l.TenantId == tenantId,
                cancellationToken);

        if (existingLink != null)
        {
            // Link already exists, no need to create duplicate
            return;
        }

        // Create new link
        var link = new NcrRiskLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NcrId = request.NcrId,
            RiskId = request.RiskId,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.UserId
        };

        _context.NcrRiskLinks.Add(link);
        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Risk.LinkedToNCR",
            "Risk",
            request.RiskId.ToString(),
            new { NcrId = request.NcrId },
            cancellationToken);
    }
}

