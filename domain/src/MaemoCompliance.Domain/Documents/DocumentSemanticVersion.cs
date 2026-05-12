namespace MaemoCompliance.Domain.Documents;

/// <summary>Major-only semantic version labels (e.g. 1 → "1.0").</summary>
public static class DocumentSemanticVersion
{
    public static string Format(int majorVersion) => $"{majorVersion}.0";
}
