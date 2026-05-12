using System.Text.RegularExpressions;

namespace MaemoCompliance.Infrastructure.Logging;

/// <summary>
/// Sanitizes log messages to prevent sensitive data leakage (PII, secrets, tokens).
/// </summary>
public static class LogSanitizer
{
    // Patterns for sensitive data
    private static readonly Regex AuthorizationHeaderRegex = new(
        @"(?i)(authorization\s*[:=]\s*)(bearer\s+)?([^\s""']+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TokenRegex = new(
        @"(?i)(token|access_token|refresh_token|id_token)\s*[:=]\s*([^\s""']+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PasswordRegex = new(
        @"(?i)(password|pwd|passwd|secret|clientsecret|apikey)\s*[:=]\s*([^\s""']+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectionStringRegex = new(
        @"(?i)(connectionstring|connection\s+string)\s*[:=]\s*([^;]+password\s*=\s*)([^;""']+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled);

    private const string MaskedValue = "***REDACTED***";
    private const int EmailMaskLength = 3; // Show first 3 chars of email

    /// <summary>
    /// Sanitizes a log message by masking sensitive data.
    /// </summary>
    public static string Sanitize(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message ?? string.Empty;
        }

        var sanitized = message;

        // Mask authorization headers
        sanitized = AuthorizationHeaderRegex.Replace(sanitized, m =>
        {
            var prefix = m.Groups[1].Value;
            var bearer = m.Groups[2].Success ? m.Groups[2].Value : string.Empty;
            return $"{prefix}{bearer}{MaskedValue}";
        });

        // Mask tokens
        sanitized = TokenRegex.Replace(sanitized, m =>
        {
            var key = m.Groups[1].Value;
            return $"{key}{MaskedValue}";
        });

        // Mask passwords and secrets
        sanitized = PasswordRegex.Replace(sanitized, m =>
        {
            var key = m.Groups[1].Value;
            return $"{key}{MaskedValue}";
        });

        // Mask connection strings (especially passwords)
        sanitized = ConnectionStringRegex.Replace(sanitized, m =>
        {
            var prefix = m.Groups[1].Value;
            var passwordKey = m.Groups[2].Value;
            return $"{prefix}{passwordKey}{MaskedValue}";
        });

        // Partially mask email addresses (show first 3 chars)
        sanitized = EmailRegex.Replace(sanitized, m =>
        {
            var email = m.Value;
            if (email.Length <= EmailMaskLength)
            {
                return MaskedValue;
            }
            var atIndex = email.IndexOf('@');
            if (atIndex > EmailMaskLength)
            {
                return email.Substring(0, EmailMaskLength) + "***@" + email.Substring(atIndex + 1);
            }
            return MaskedValue;
        });

        return sanitized;
    }

    /// <summary>
    /// Sanitizes an object's properties by masking sensitive fields.
    /// </summary>
    public static object SanitizeObject(object? obj)
    {
        if (obj == null)
        {
            return obj!;
        }

        // For simple types, return as-is
        if (obj is string str)
        {
            return Sanitize(str);
        }

        if (obj is not System.Collections.IDictionary dict)
        {
            // For complex objects, serialize to string and sanitize
            // This is a simple approach - in production, you might want more sophisticated handling
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(obj);
                var sanitizedJson = Sanitize(json);
                return System.Text.Json.JsonSerializer.Deserialize<object>(sanitizedJson) ?? obj;
            }
            catch
            {
                // If serialization fails, return a safe representation
                return "{***REDACTED***}";
            }
        }

        // For dictionaries, sanitize values
        var sanitizedDict = new Dictionary<string, object?>();
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            var key = entry.Key?.ToString() ?? "unknown";
            var value = entry.Value;

            // Skip sensitive keys entirely or mask their values
            if (IsSensitiveKey(key))
            {
                sanitizedDict[key] = MaskedValue;
            }
            else if (value is string strValue)
            {
                sanitizedDict[key] = Sanitize(strValue);
            }
            else
            {
                sanitizedDict[key] = value;
            }
        }

        return sanitizedDict;
    }

    private static bool IsSensitiveKey(string key)
    {
        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("password") ||
               lowerKey.Contains("secret") ||
               lowerKey.Contains("token") ||
               lowerKey.Contains("authorization") ||
               lowerKey.Contains("apikey") ||
               lowerKey.Contains("clientsecret") ||
               lowerKey.Contains("connectionstring");
    }
}

