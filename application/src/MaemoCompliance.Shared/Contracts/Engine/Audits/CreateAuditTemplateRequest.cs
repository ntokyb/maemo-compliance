namespace MaemoCompliance.Shared.Contracts.Engine.Audits;

/// <summary>
/// Request DTO for creating an audit template in the Engine API.
/// </summary>
public class CreateAuditTemplateRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

