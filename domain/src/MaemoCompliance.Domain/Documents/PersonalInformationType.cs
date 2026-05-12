namespace MaemoCompliance.Domain.Documents;

/// <summary>
/// Personal Information Type classification for POPIA compliance.
/// Used to classify documents by the type of personal information they contain.
/// </summary>
public enum PersonalInformationType
{
    /// <summary>
    /// Document does not contain personal information.
    /// </summary>
    None = 0,

    /// <summary>
    /// Document contains personal information (names, IDs, addresses, contact details).
    /// </summary>
    PersonalInfo = 1,

    /// <summary>
    /// Document contains special personal information (health, biometrics, religion, minors, etc.).
    /// Requires enhanced protection under POPIA.
    /// </summary>
    SpecialPersonalInfo = 2
}

