using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AccessRequests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.AccessRequests.Queries;

public class GetPendingAccessRequestCountQueryHandler : IRequestHandler<GetPendingAccessRequestCountQuery, int>
{
    private readonly IApplicationDbContext _context;

    public GetPendingAccessRequestCountQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> Handle(GetPendingAccessRequestCountQuery request, CancellationToken cancellationToken) =>
        _context.AccessRequests.CountAsync(a => a.Status == AccessRequestStatus.Pending, cancellationToken);
}
