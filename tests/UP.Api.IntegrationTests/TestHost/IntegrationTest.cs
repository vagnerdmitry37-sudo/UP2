using Microsoft.Extensions.DependencyInjection;

using UP.Infrastructure.Persistence;

namespace UP.Api.IntegrationTests.TestHost;

[Collection(ApiCollectionDefinition.Name)]
public abstract class IntegrationTest(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<HttpClient> _clients = [];

    protected ApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateHttpsClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        _clients.ForEach(client => client.Dispose());
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected HttpClient CreateSessionClient() => Track(Factory.CreateHttpsClient());

    protected HttpClient CreateClientWithoutCookies() => Track(Factory.CreateHttpsClient(handleCookies: false));

    protected async Task<T> QueryDatabaseAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private HttpClient Track(HttpClient client)
    {
        _clients.Add(client);
        return client;
    }
}
