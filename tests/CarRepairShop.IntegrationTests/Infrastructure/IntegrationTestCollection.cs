namespace CarRepairShop.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Integration Tests";
}
