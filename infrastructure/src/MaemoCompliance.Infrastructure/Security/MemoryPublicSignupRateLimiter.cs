using System.Collections.Concurrent;
using MaemoCompliance.Application.Common;

namespace MaemoCompliance.Infrastructure.Security;

/// <summary>
/// Allows up to <paramref name="maxRequests"/> per rolling hour per key (e.g. client IP).
/// </summary>
public sealed class MemoryPublicSignupRateLimiter : IPublicSignupRateLimiter
{
    private readonly int _maxRequests;
    private readonly ConcurrentDictionary<string, List<DateTime>> _windows = new();

    public MemoryPublicSignupRateLimiter(int maxRequestsPerHour = 5)
    {
        _maxRequests = maxRequestsPerHour;
    }

    public bool TryAllow(string clientKey)
    {
        var now = DateTime.UtcNow;
        var list = _windows.GetOrAdd(clientKey, _ => new List<DateTime>());
        lock (list)
        {
            list.RemoveAll(t => now - t > TimeSpan.FromHours(1));
            if (list.Count >= _maxRequests)
            {
                return false;
            }

            list.Add(now);
            return true;
        }
    }
}
