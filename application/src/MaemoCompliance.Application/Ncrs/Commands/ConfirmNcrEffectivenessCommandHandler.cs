using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public sealed class ConfirmNcrEffectivenessCommandHandler : IRequestHandler<ConfirmNcrEffectivenessCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmNcrEffectivenessCommandHandler(
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

    public async Task Handle(ConfirmNcrEffectivenessCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var ncr = await _context.Ncrs
            .FirstOrDefaultAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (ncr == null)
        {
            throw new KeyNotFoundException($"NCR with Id {request.NcrId} was not found.");
        }

        if (!ncr.CorrectiveActionCompletedAt.HasValue)
        {
            throw new InvalidOperationException("Corrective action must be completed before effectiveness can be confirmed.");
        }

        ncr.EffectivenessConfirmed = true;
        ncr.EffectivenessVerifiedAt = _dateTimeProvider.UtcNow;
        ncr.ModifiedAt = _dateTimeProvider.UtcNow;
        ncr.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
