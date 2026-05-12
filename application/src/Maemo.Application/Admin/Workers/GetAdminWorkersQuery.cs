using MediatR;

namespace Maemo.Application.Admin.Workers;

public sealed record GetAdminWorkersQuery() : IRequest<IReadOnlyList<AdminWorkerSummaryDto>>;

