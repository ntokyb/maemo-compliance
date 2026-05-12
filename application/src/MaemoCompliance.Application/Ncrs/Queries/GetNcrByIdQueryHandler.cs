using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Ncrs.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrByIdQueryHandler : IRequestHandler<GetNcrByIdQuery, NcrDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetNcrByIdQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<NcrDto?> Handle(GetNcrByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        return await _context.Ncrs
            .Where(n => n.Id == request.Id && n.TenantId == tenantId)
            .Select(n => new NcrDto
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                Department = n.Department,
                OwnerUserId = n.OwnerUserId,
                Severity = n.Severity,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                DueDate = n.DueDate,
                ClosedAt = n.ClosedAt,
                Category = n.Category,
                RootCause = n.RootCause,
                CorrectiveAction = n.CorrectiveAction,
                EscalationLevel = n.EscalationLevel
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}

