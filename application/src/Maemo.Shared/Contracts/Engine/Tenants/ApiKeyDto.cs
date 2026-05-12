namespace Maemo.Shared.Contracts.Engine.Tenants;

/// <summary>
/// DTO for API key information in the Engine API (without exposing the actual key value).
/// </summary>
public class ApiKeyDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

