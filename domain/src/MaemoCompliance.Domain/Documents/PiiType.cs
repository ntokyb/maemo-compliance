namespace MaemoCompliance.Domain.Documents;

/// <summary>
/// Personal Information Type classification for POPIA compliance.
/// Used to classify documents by the specific type of personal information they contain.
/// </summary>
public enum PiiType
{
    /// <summary>
    /// Document does not contain personal information.
    /// </summary>
    None = 0,

    /// <summary>
    /// Document contains general personal information (names, IDs, addresses, contact details).
    /// </summary>
    PersonalInfo = 1,

    /// <summary>
    /// Document contains special personal information (biometric, religious, political, etc.).
    /// Requires enhanced protection under POPIA.
    /// </summary>
    SpecialPersonalInfo = 2,

    /// <summary>
    /// Document contains information about children (minors under 18).
    /// Requires enhanced protection under POPIA.
    /// </summary>
    Children = 3,

    /// <summary>
    /// Document contains financial information (bank accounts, credit cards, financial records).
    /// </summary>
    Financial = 4,

    /// <summary>
    /// Document contains health information (medical records, health status, treatments).
    /// Requires enhanced protection under POPIA.
    /// </summary>
    Health = 5
}

