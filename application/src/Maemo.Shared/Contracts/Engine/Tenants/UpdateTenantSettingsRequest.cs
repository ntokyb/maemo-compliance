namespace Maemo.Shared.Contracts.Engine.Tenants;

/// <summary>
/// Request DTO for updating tenant settings in the Engine API.
/// </summary>
public class UpdateTenantSettingsRequest
{
    public string Name { get; set; } = null!;
    public string? Domain { get; set; }
    public string AdminEmail { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Plan { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}

