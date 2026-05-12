namespace Maemo.IntegrationTests.Fixtures;

/// <summary>
/// Single shared API fixture with sequential test execution (avoids parallel IAsyncLifetime resets on shared DB state).
/// </summary>
[CollectionDefinition("MaemoApi", DisableParallelization = true)]
public class MaemoApiCollection : ICollectionFixture<MaemoApiFixture>
{
}
