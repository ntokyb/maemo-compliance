namespace Maemo.Application.Security.Dtos;

/// <summary>
/// DTO for API key information (without exposing the actual key value).
/// </summary>
public class ApiKeyDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new API key (includes the key value only on creation).
/// </summary>
public class CreateApiKeyRequest
{
    public string? Name { get; set; }
}

/// <summary>
/// DTO returned when creating a new API key (includes the key value).
/// </summary>
public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = null!;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

