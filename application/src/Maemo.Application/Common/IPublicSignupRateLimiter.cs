namespace Maemo.Application.Common;

/// <summary>
/// In-memory rate limit for unauthenticated signup. Production: prefer edge/CDN or API gateway limits.
/// </summary>
public interface IPublicSignupRateLimiter
{
    /// <summary>Returns false when the client has exceeded the limit.</summary>
    bool TryAllow(string clientKey);
}
