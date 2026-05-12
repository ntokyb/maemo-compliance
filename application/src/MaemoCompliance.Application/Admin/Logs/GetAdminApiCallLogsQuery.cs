using MediatR;

namespace MaemoCompliance.Application.Admin.Logs;

/// <summary>
/// Query to get API call logs with optional date filtering.
/// </summary>
public sealed record GetAdminApiCallLogsQuery(
    DateTime? From,
    DateTime? To,
    int Limit = 100
) : IRequest<IReadOnlyList<AdminApiCallLogDto>>;

