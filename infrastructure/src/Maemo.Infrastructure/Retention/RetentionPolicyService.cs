using Maemo.Application.Common;

namespace Maemo.Infrastructure.Retention;

/// <summary>
/// Implementation of retention policy service with hardcoded defaults.
/// Can be extended later to use database-backed configuration.
/// </summary>
public class RetentionPolicyService : IRetentionPolicyService
{
    public DateTime? CalculateRetainUntil(string? documentCategory, string? department)
    {
        // Default retention period: 5 years from now
        var defaultRetentionYears = 5;
        var baseDate = DateTime.UtcNow;

        // If no category specified, use default
        if (string.IsNullOrWhiteSpace(documentCategory))
        {
            return baseDate.AddYears(defaultRetentionYears);
        }

        // Category-based policies (case-insensitive)
        var categoryLower = documentCategory.Trim().ToLowerInvariant();

        // Finance documents: 5 years
        if (categoryLower.Contains("finance") || categoryLower.Contains("financial"))
        {
            return baseDate.AddYears(5);
        }

        // HR documents: 7 years (longer retention for personnel records)
        if (categoryLower.Contains("hr") || categoryLower.Contains("human resources") || categoryLower.Contains("personnel"))
        {
            return baseDate.AddYears(7);
        }

        // Legal documents: 7 years
        if (categoryLower.Contains("legal") || categoryLower.Contains("contract"))
        {
            return baseDate.AddYears(7);
        }

        // Tax documents: 5 years
        if (categoryLower.Contains("tax"))
        {
            return baseDate.AddYears(5);
        }

        // Audit documents: 7 years
        if (categoryLower.Contains("audit"))
        {
            return baseDate.AddYears(7);
        }

        // Compliance documents: 5 years
        if (categoryLower.Contains("compliance") || categoryLower.Contains("regulatory"))
        {
            return baseDate.AddYears(5);
        }

        // BBBEE Certificate: 3 years (typically renewed annually)
        if (categoryLower.Contains("bbbee") || categoryLower.Contains("b-bbee"))
        {
            return baseDate.AddYears(3);
        }

        // Permanent retention for certain document types
        if (categoryLower.Contains("permanent") || categoryLower.Contains("archive"))
        {
            return null; // null means permanent retention
        }

        // Default: 5 years
        return baseDate.AddYears(defaultRetentionYears);
    }
}

