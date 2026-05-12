using MaemoCompliance.Domain.Ncrs;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Filter criteria for listing NCRs.
/// </summary>
public class NcrFilter
{
    public NcrStatus? Status { get; set; }
    public NcrSeverity? Severity { get; set; }
    public string? Department { get; set; }
}

