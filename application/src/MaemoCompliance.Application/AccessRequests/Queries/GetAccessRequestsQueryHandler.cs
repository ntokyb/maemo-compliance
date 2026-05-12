using MaemoCompliance.Application.AccessRequests.Dtos;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AccessRequests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.AccessRequests.Queries;

public class GetAccessRequestsQueryHandler : IRequestHandler<GetAccessRequestsQuery, IReadOnlyList<AccessRequestListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAccessRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AccessRequestListDto>> Handle(
        GetAccessRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var q = _context.AccessRequests.AsNoTracking().OrderByDescending(a => a.CreatedAt);

        List<AccessRequest> list;
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            list = await q.ToListAsync(cancellationToken);
        }
        else if (Enum.TryParse<AccessRequestStatus>(request.Status, true, out var st))
        {
            list = await q.Where(a => a.Status == st).ToListAsync(cancellationToken);
        }
        else
        {
            list = await q.ToListAsync(cancellationToken);
        }

        return list.Select(a => new AccessRequestListDto
        {
            Id = a.Id,
            CompanyName = a.CompanyName,
            Industry = a.Industry,
            ContactName = a.ContactName,
            ContactEmail = a.ContactEmail,
            TargetStandardsSummary = Truncate(a.TargetStandardsJson, 120),
            CreatedAt = a.CreatedAt,
            Status = a.Status.ToString()
        }).ToList();
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
        {
            return s;
        }

        return s[..max] + "…";
    }
}
