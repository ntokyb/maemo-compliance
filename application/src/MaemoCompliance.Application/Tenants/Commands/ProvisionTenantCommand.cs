using MediatR;

namespace MaemoCompliance.Application.Tenants.Commands;

public class ProvisionTenantCommand : IRequest<Guid>
{
    public string Name { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public string Plan { get; set; } = null!;
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>Optional self-service profile — triggers extended onboarding seed when ISO list is non-empty.</summary>
    public string? Industry { get; set; }

    public IReadOnlyList<string>? IsoFrameworks { get; set; }

    /// <summary>When true, default modules are applied for new workspaces (Documents, NCR, Risks, Audits).</summary>
    public bool EnableDefaultComplianceModules { get; set; }
}

