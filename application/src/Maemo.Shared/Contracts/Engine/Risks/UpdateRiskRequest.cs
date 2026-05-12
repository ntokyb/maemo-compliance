using Maemo.Domain.Risks;

namespace Maemo.Shared.Contracts.Engine.Risks;

/// <summary>
/// Request DTO for updating an existing risk in the Engine API.
/// </summary>
public class UpdateRiskRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public RiskCategory Category { get; set; }
    public string? Cause { get; set; }
    public string? Consequences { get; set; }
    public int InherentLikelihood { get; set; }
    public int InherentImpact { get; set; }
    public string? ExistingControls { get; set; }
    public int ResidualLikelihood { get; set; }
    public int ResidualImpact { get; set; }
    public string? OwnerUserId { get; set; }
    public RiskStatus Status { get; set; }
}

