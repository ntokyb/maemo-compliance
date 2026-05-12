using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Risks;

public class Risk : TenantOwnedEntity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public RiskCategory Category { get; set; }
    public string? Cause { get; set; }
    public string? Consequences { get; set; }
    public int InherentLikelihood { get; set; } // 1-5
    public int InherentImpact { get; set; } // 1-5
    public int InherentScore { get; set; } // Computed: InherentLikelihood * InherentImpact
    public string? ExistingControls { get; set; }
    public int ResidualLikelihood { get; set; } // 1-5
    public int ResidualImpact { get; set; } // 1-5
    public int ResidualScore { get; set; } // Computed: ResidualLikelihood * ResidualImpact
    public string? OwnerUserId { get; set; }
    public RiskStatus Status { get; set; }
}

