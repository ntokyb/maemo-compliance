using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrListQueryHandler : IRequestHandler<GetNcrListQuery, IReadOnlyList<NcrDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetNcrListQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<NcrDto>> Handle(GetNcrListQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var query = _context.Ncrs
            .Where(n => n.TenantId == tenantId);

        if (request.Status.HasValue)
        {
            query = query.Where(n => n.Status == request.Status.Value);
        }

        if (request.Severity.HasValue)
        {
            query = query.Where(n => n.Severity == request.Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(n => n.Department == request.Department);
        }

        return await query
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
            .ToListAsync(cancellationToken);
    }
}

