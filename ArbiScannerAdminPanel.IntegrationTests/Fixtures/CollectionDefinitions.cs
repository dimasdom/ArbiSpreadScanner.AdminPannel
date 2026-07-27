namespace ArbiScannerAdminPanel.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class AdminApiCollectionDefinition : ICollectionFixture<AdminApiTestFixture>
{
    public const string Name = "AdminApi integration tests";
}
