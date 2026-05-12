namespace MaemoCompliance.IntegrationTests.Fixtures;

/// <summary>
/// Single shared API fixture with sequential test execution (avoids parallel IAsyncLifetime resets on shared DB state).
/// </summary>
[CollectionDefinition("MaemoComplianceApi", DisableParallelization = true)]
public class MaemoComplianceApiCollection : ICollectionFixture<MaemoComplianceApiFixture>
{
}
