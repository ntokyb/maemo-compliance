namespace MaemoCompliance.Application.Demo;

/// <summary>
/// Service for seeding demo data - creates a demo tenant with sample documents, NCRs, risks, and audits.
/// </summary>
public interface IDemoDataSeeder
{
    /// <summary>
    /// Seeds demo data if it doesn't already exist. Idempotent operation.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds demo tenant/data when not already present; returns whether data existed beforehand.
    /// </summary>
    Task<DemoSeedResult> SeedDemoWithOutcomeAsync(CancellationToken cancellationToken = default);
}

