using MediatR;

namespace MaemoCompliance.Application.Admin.Workers;

public sealed record GetAdminWorkersQuery() : IRequest<IReadOnlyList<AdminWorkerSummaryDto>>;

