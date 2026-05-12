using MaemoCompliance.Domain.Risks;

namespace MaemoCompliance.Shared.Contracts.Engine.Risks;

/// <summary>
/// Filter criteria for listing risks in the Engine API.
/// </summary>
public class RiskFilter
{
    public RiskCategory? Category { get; set; }
    public RiskStatus? Status { get; set; }
}

