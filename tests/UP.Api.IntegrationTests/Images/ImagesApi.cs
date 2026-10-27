using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using UP.Api.Images;
using UP.Domain.Images;

namespace UP.Api.IntegrationTests.Images;

internal static class ImagesApi
{
    public const string ImagesRoute = "api/images";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Image NewImage(Guid entityId, int sortOrder) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EntityType = ImageEntityType.Product,
            EntityId = entityId,
            Key = $"Product/{entityId}/{Guid.CreateVersion7()}.webp",
            SortOrder = sortOrder,
        };

    public static (byte[] Content, string ContentType) NewFile(
        string contentType = "image/jpeg",
        int length = 1024) =>
        ([.. Enumerable.Range(0, length).Select(index => (byte)index)], contentType);

    public static async Task<HttpResponseMessage> CreateImagesAsync(
        this HttpClient client,
        string? accessToken,
        Guid entityId,
        params (byte[] Content, string ContentType)[] files)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Product"), "entityType" },
            { new StringContent(entityId.ToString()), "entityId" },
        };

        foreach ((byte[] content, string contentType) in files)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "files", "photo");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ImagesRoute) { Content = form };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static Task<HttpResponseMessage> GetImagesAsync(
        this HttpClient client,
        string entityType,
        params Guid[] entityIds) =>
        client.GetAsync(
            $"{ImagesRoute}?entityType={entityType}{string.Concat(entityIds.Select(id => $"&entityIds={id}"))}",
            CancellationToken);

    public static async Task<List<ImageResponse>> ReadImagesAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<ImageResponse>>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no images.");
}
