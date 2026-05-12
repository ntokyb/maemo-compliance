using MediatR;

namespace MaemoCompliance.Application.Admin.Logs;

/// <summary>
/// Query to get webhook delivery logs with optional filtering.
/// </summary>
public sealed record GetAdminWebhookDeliveryLogsQuery(
    DateTime? From,
    DateTime? To,
    bool FailedOnly = false,
    int Limit = 100
) : IRequest<IReadOnlyList<AdminWebhookDeliveryLogDto>>;

