namespace Maemo.Application.Tenants.Dtos;

public sealed class TenantLicenseSettingsDto
{
    public string SubscriptionPlan { get; init; } = null!;
    public string? Edition { get; init; }
    public int MaxUsers { get; init; }
    public long MaxStorageBytes { get; init; }
    public DateTime? SubscriptionExpiresAt { get; init; }
    public IReadOnlyList<string> EnabledModules { get; init; } = Array.Empty<string>();
}
