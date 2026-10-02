using Microsoft.Extensions.DependencyInjection;

using UP.Infrastructure.Persistence;

namespace UP.Api.IntegrationTests.TestHost;

[Collection(ApiCollectionDefinition.Name)]
public abstract class IntegrationTest(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateHttpsClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected async Task<T> QueryDatabaseAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
