using Maemo.Application.Common;
using Maemo.Domain.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Logs;

/// <summary>
/// Handler for GetAdminWebhookDeliveryLogsQuery - retrieves webhook delivery logs.
/// </summary>
public class GetAdminWebhookDeliveryLogsQueryHandler : IRequestHandler<GetAdminWebhookDeliveryLogsQuery, IReadOnlyList<AdminWebhookDeliveryLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminWebhookDeliveryLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminWebhookDeliveryLogDto>> Handle(GetAdminWebhookDeliveryLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WebhookDeliveryLogs.AsQueryable();

        // Apply date filters
        if (request.From.HasValue)
        {
            query = query.Where(log => log.Timestamp >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(log => log.Timestamp <= request.To.Value);
        }

        // Apply failed-only filter
        if (request.FailedOnly)
        {
            query = query.Where(log => !log.Success);
        }

        // Order by timestamp descending and limit
        var logs = await query
            .OrderByDescending(log => log.Timestamp)
            .Take(request.Limit)
            .Select(log => new AdminWebhookDeliveryLogDto(
                log.Id,
                log.TenantId,
                log.TenantName,
                log.EventType,
                log.TargetUrl,
                log.Success,
                log.Timestamp,
                log.StatusCode,
                log.ErrorMessage
            ))
            .ToListAsync(cancellationToken);

        return logs;
    }
}

