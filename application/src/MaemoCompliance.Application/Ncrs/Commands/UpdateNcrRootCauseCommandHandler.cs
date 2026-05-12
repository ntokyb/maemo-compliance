using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Application.Ncrs.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Commands;

public sealed class UpdateNcrRootCauseCommandHandler : IRequestHandler<UpdateNcrRootCauseCommand, NcrDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public UpdateNcrRootCauseCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<NcrDto> Handle(UpdateNcrRootCauseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var ncr = await _context.Ncrs
            .FirstOrDefaultAsync(n => n.Id == request.NcrId && n.TenantId == tenantId, cancellationToken);

        if (ncr == null)
        {
            throw new KeyNotFoundException($"NCR with Id {request.NcrId} was not found.");
        }

        ncr.RootCauseMethod = request.RootCauseMethod;
        ncr.RootCause = request.RootCause;
        ncr.CorrectiveActionPlan = request.CorrectiveActionPlan;
        ncr.CorrectiveActionOwner = request.CorrectiveActionOwner;
        ncr.CorrectiveActionDueDate = request.CorrectiveActionDueDate;
        ncr.ModifiedAt = _dateTimeProvider.UtcNow;
        ncr.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return (await _mediator.Send(new GetNcrByIdQuery { Id = ncr.Id }, cancellationToken))!;
    }
}
