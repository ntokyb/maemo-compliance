namespace Maemo.Shared.Contracts.Engine.Tenants;

/// <summary>
/// Request DTO for creating a new API key in the Engine API.
/// </summary>
public class CreateApiKeyRequest
{
    public string? Name { get; set; }
}

