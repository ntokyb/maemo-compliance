namespace MaemoCompliance.Shared.Contracts.Engine.Tenants;

/// <summary>
/// Response DTO returned when creating a new API key in the Engine API.
/// Includes the key value which is only returned on creation.
/// </summary>
public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = null!;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

