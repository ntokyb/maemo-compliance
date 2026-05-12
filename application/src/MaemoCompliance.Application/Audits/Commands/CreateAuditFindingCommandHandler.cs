using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Audits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Commands;

public sealed class CreateAuditFindingCommandHandler : IRequestHandler<CreateAuditFindingCommand, AuditFindingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public CreateAuditFindingCommandHandler(
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

    public async Task<AuditFindingDto> Handle(CreateAuditFindingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var runExists = await _context.AuditRuns
            .AnyAsync(r => r.Id == request.AuditRunId && r.TenantId == tenantId, cancellationToken);

        if (!runExists)
        {
            throw new KeyNotFoundException($"Audit run {request.AuditRunId} was not found.");
        }

        var now = _dateTimeProvider.UtcNow;
        var finding = new AuditFinding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuditRunId = request.AuditRunId,
            Title = request.Title.Trim(),
            CreatedAt = now,
            CreatedBy = _currentUserService.UserId,
        };

        _context.AuditFindings.Add(finding);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuditFindingDto
        {
            Id = finding.Id,
            AuditRunId = finding.AuditRunId,
            Title = finding.Title,
            LinkedNcrId = finding.LinkedNcrId,
        };
    }
}
