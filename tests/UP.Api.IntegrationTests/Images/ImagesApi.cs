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
