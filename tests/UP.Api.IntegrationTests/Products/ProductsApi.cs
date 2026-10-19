using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using UP.Api.Products;

namespace UP.Api.IntegrationTests.Products;

internal static class ProductsApi
{
    public const string ProductsRoute = "api/products";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static object NewProduct(
        string type = "Excursion",
        string name = "Old Town Walk",
        decimal price = 25.50m,
        object? attributes = null) =>
        new
        {
            type,
            name,
            description = "Three hours through the historic centre",
            price,
            attributes = attributes ?? new { durationMinutes = 180, meetingPoint = "Main square" },
        };

    public static async Task<HttpResponseMessage> CreateProductAsync(
        this HttpClient client,
        string? accessToken,
        object product)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ProductsRoute)
        {
            Content = JsonContent.Create(product),
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<ProductResponse> ReadProductAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no product.");
}
