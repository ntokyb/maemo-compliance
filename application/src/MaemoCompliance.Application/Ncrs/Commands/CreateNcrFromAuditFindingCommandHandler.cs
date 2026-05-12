using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public sealed class CreateNcrFromAuditFindingCommandHandler : IRequestHandler<CreateNcrFromAuditFindingCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public CreateNcrFromAuditFindingCommandHandler(
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

    public async Task<Guid> Handle(CreateNcrFromAuditFindingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var finding = await _context.AuditFindings
            .FirstOrDefaultAsync(
                f => f.Id == request.FindingId && f.TenantId == tenantId && f.AuditRunId == request.AuditRunId,
                cancellationToken);

        if (finding == null)
        {
            throw new KeyNotFoundException("Audit finding was not found for this audit run.");
        }

        if (finding.LinkedNcrId.HasValue)
        {
            throw new InvalidOperationException("An NCR has already been created for this finding.");
        }

        var now = _dateTimeProvider.UtcNow;
        var ncr = new Ncr
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = $"NCR from audit finding: {finding.Title}",
            Description = finding.Title,
            Severity = NcrSeverity.Medium,
            Status = NcrStatus.Open,
            CreatedAt = now,
            CreatedBy = _currentUserService.UserId,
            LinkedAuditFindingId = finding.Id,
        };

        _context.Ncrs.Add(ncr);
        finding.LinkedNcrId = ncr.Id;
        finding.ModifiedAt = now;
        finding.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return ncr.Id;
    }
}
