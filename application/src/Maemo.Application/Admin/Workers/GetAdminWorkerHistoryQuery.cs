using MediatR;

namespace Maemo.Application.Admin.Workers;

public sealed record GetAdminWorkerHistoryQuery(string WorkerName, int Limit = 50) : IRequest<IReadOnlyList<AdminWorkerJobHistoryItemDto>>;

