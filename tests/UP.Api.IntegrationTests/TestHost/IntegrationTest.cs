using System.Net;

using Amazon.S3;
using Amazon.S3.Model;

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

    protected async Task<StoredFile?> ReadStoredFileAsync(string key)
    {
        try
        {
            using GetObjectResponse stored = await Factory.Services.GetRequiredService<IAmazonS3>()
                .GetObjectAsync(ApiFactory.ImagesBucket, key, CancellationToken);
            using var content = new MemoryStream();
            await stored.ResponseStream.CopyToAsync(content, CancellationToken);
            return new StoredFile(content.ToArray(), stored.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    protected async Task<int> CountStoredFilesAsync(string keyPrefix)
    {
        ListObjectsV2Response listing = await Factory.Services.GetRequiredService<IAmazonS3>().ListObjectsV2Async(
            new ListObjectsV2Request { BucketName = ApiFactory.ImagesBucket, Prefix = keyPrefix },
            CancellationToken);
        return listing.S3Objects?.Count ?? 0;
    }

    private HttpClient Track(HttpClient client)
    {
        _clients.Add(client);
        return client;
    }
}
