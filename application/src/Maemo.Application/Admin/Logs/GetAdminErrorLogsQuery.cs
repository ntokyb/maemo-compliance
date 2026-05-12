using MediatR;

namespace Maemo.Application.Admin.Logs;

/// <summary>
/// Query to get error logs with optional date filtering.
/// </summary>
public sealed record GetAdminErrorLogsQuery(
    DateTime? From,
    DateTime? To,
    int Limit = 100
) : IRequest<IReadOnlyList<AdminErrorLogDto>>;

