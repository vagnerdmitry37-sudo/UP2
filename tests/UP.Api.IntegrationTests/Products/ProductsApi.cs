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
        object? attributes = null,
        Guid? id = null) =>
        new
        {
            id,
            type,
            name,
            description = "Three hours through the historic centre",
            price,
            attributes = attributes ?? new { durationMinutes = 180, meetingPoint = "Main square" },
        };

    public static Task<HttpResponseMessage> CreateProductsAsync(
        this HttpClient client,
        string? accessToken,
        params object[] products) =>
        client.SendProductsAsync(HttpMethod.Post, accessToken, products);

    public static Task<HttpResponseMessage> UpdateProductsAsync(
        this HttpClient client,
        string? accessToken,
        params object[] products) =>
        client.SendProductsAsync(HttpMethod.Put, accessToken, products);

    public static Task<HttpResponseMessage> GetProductsAsync(this HttpClient client, params Guid[] ids) =>
        client.GetAsync(
            ids.Length == 0 ? ProductsRoute : $"{ProductsRoute}?{string.Join("&", ids.Select(id => $"ids={id}"))}",
            CancellationToken);

    public static async Task<List<ProductResponse>> ReadProductsAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<ProductResponse>>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no products.");

    private static async Task<HttpResponseMessage> SendProductsAsync(
        this HttpClient client,
        HttpMethod method,
        string? accessToken,
        object[] products)
    {
        using var request = new HttpRequestMessage(method, ProductsRoute)
        {
            Content = JsonContent.Create(products),
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}
