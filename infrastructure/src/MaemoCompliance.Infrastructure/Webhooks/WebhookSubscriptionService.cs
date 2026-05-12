using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Webhooks;
using MaemoCompliance.Domain.Webhooks;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Infrastructure.Webhooks;

/// <summary>
/// Implementation of webhook subscription service.
/// </summary>
public class WebhookSubscriptionService : IWebhookSubscriptionService
{
    private readonly MaemoComplianceDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WebhookSubscriptionService(MaemoComplianceDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WebhookSubscription> CreateAsync(Guid tenantId, string url, string eventType, string? secret, CancellationToken cancellationToken = default)
    {
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Url = url,
            EventType = eventType,
            Secret = secret,
            IsActive = true,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = "System"
        };

        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return subscription;
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.EventType)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetByTenantAndEventTypeAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.EventType == eventType && s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);

        if (subscription != null)
        {
            subscription.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

