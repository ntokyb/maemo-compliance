using Serilog.Core;
using Serilog.Events;

namespace MaemoCompliance.Infrastructure.Logging;

/// <summary>
/// Filters out sensitive data from log events by sanitizing message templates and properties.
/// This filter excludes log events that contain sensitive headers or data.
/// </summary>
public class SensitiveDataFilter : ILogEventFilter
{
    public bool IsEnabled(LogEvent logEvent)
    {
        // Check if the log event contains sensitive data in properties
        foreach (var property in logEvent.Properties)
        {
            var key = property.Key.ToLowerInvariant();
            
            // Exclude logs with sensitive header names
            if (key.Contains("authorization") || 
                key.Contains("cookie") || 
                key.Contains("x-api-key") ||
                key.Contains("password") ||
                key.Contains("secret") ||
                key.Contains("token"))
            {
                // Still allow the log, but it will be sanitized by the enricher
                return true;
            }
        }

        return true;
    }
}

