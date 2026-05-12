namespace Maemo.Application.Common;

/// <summary>
/// Service for calculating document retention dates based on category and department.
/// </summary>
public interface IRetentionPolicyService
{
    /// <summary>
    /// Calculates the retention date (RetainUntil) for a document based on its category and department.
    /// </summary>
    /// <param name="documentCategory">The document category (e.g., "Finance", "HR")</param>
    /// <param name="department">The department (optional)</param>
    /// <returns>The date until which the document should be retained, or null if no policy applies</returns>
    DateTime? CalculateRetainUntil(string? documentCategory, string? department);
}

