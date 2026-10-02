namespace UP.Api.IntegrationTests.TestHost;

[CollectionDefinition(Name)]
public sealed class ApiCollectionDefinition : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
