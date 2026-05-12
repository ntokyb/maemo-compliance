namespace Maemo.Application.Onboarding.Dtos;

/// <summary>
/// Request for tenant onboarding wizard.
/// </summary>
public class OnboardingRequest
{
    /// <summary>
    /// Selected ISO standards (multi-select).
    /// </summary>
    public List<string> IsoStandards { get; set; } = new();

    /// <summary>
    /// Selected industry.
    /// </summary>
    public string Industry { get; set; } = null!;

    /// <summary>
    /// Company size.
    /// </summary>
    public string CompanySize { get; set; } = null!;
}

