namespace MaemoCompliance.Application.Demo;

/// <summary>
/// Outcome of development demo seed endpoint (idempotent).
/// </summary>
public sealed class DemoSeedResult
{
    public bool WasAlreadySeeded { get; init; }
    public Guid TenantId { get; init; }
    public string AdminEmail { get; init; } = null!;
}
