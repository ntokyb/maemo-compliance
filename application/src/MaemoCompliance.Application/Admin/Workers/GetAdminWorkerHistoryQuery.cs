using MediatR;

namespace MaemoCompliance.Application.Admin.Workers;

public sealed record GetAdminWorkerHistoryQuery(string WorkerName, int Limit = 50) : IRequest<IReadOnlyList<AdminWorkerJobHistoryItemDto>>;

