using MediatR;

namespace MaemoCompliance.Application.Admin.Logs;

/// <summary>
/// Query to get worker job logs with optional filtering.
/// </summary>
public sealed record GetAdminWorkerJobLogsQuery(
    DateTime? From,
    DateTime? To,
    bool FailedOnly = false,
    string? WorkerName = null,
    int Limit = 100
) : IRequest<IReadOnlyList<AdminWorkerJobLogDto>>;

