using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Ncrs;

public class NcrRiskLink : TenantOwnedEntity
{
    public Guid NcrId { get; set; }
    public Guid RiskId { get; set; }
}

