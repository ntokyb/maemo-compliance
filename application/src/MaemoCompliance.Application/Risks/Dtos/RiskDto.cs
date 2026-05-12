using MaemoCompliance.Domain.Risks;

namespace MaemoCompliance.Application.Risks.Dtos;

public class RiskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public RiskCategory Category { get; set; }
    public string? Cause { get; set; }
    public string? Consequences { get; set; }
    public int InherentLikelihood { get; set; }
    public int InherentImpact { get; set; }
    public int InherentScore { get; set; }
    public string? ExistingControls { get; set; }
    public int ResidualLikelihood { get; set; }
    public int ResidualImpact { get; set; }
    public int ResidualScore { get; set; }
    public string? OwnerUserId { get; set; }
    public RiskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    
    // Computed property for risk level
    public string RiskLevel => GetRiskLevel(ResidualScore);
    
    private static string GetRiskLevel(int score)
    {
        return score switch
        {
            <= 5 => "Low",
            <= 10 => "Medium",
            <= 15 => "High",
            _ => "Critical"
        };
    }
}

public class CreateRiskRequest
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
    public RiskStatus Status { get; set; } = RiskStatus.Identified;
}

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

