namespace MaemoCompliance.Domain.Documents;

/// <summary>
/// Personal Information (PII) data type classification for POPIA compliance.
/// </summary>
public enum PiiDataType
{
    /// <summary>
    /// Document does not contain personal information.
    /// </summary>
    None = 0,

    /// <summary>
    /// Document contains personal information (e.g., names, ID numbers, contact details).
    /// </summary>
    Personal = 1,

    /// <summary>
    /// Document contains special personal information (e.g., health, biometric, financial data).
    /// Requires enhanced protection under POPIA.
    /// </summary>
    SpecialPersonal = 2
}

