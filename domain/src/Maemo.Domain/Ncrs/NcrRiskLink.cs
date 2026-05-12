using Maemo.Domain.Common;

namespace Maemo.Domain.Ncrs;

public class NcrRiskLink : TenantOwnedEntity
{
    public Guid NcrId { get; set; }
    public Guid RiskId { get; set; }
}

