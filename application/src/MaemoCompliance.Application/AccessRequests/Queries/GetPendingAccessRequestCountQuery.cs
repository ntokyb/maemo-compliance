using MediatR;

namespace MaemoCompliance.Application.AccessRequests.Queries;

public sealed record GetPendingAccessRequestCountQuery : IRequest<int>;
