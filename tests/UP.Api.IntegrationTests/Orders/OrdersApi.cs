using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using UP.Api.Orders;

namespace UP.Api.IntegrationTests.Orders;

internal static class OrdersApi
{
    public const string OrdersRoute = "api/orders";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static object NewItem(Guid productId, int passengers = 2, DateTimeOffset? startsAt = null) =>
        new
        {
            productId,
            startsAt = startsAt ?? DateTimeOffset.UtcNow.AddDays(7),
            passengers,
            pickupAddress = "Hotel Pupp, Karlovy Vary",
        };

    public static object NewOrder(string customerName, params object[] items) =>
        new
        {
            customerName,
            customerEmail = "anna@example.com",
            customerPhone = "+420 600 000 000",
            items,
        };

    public static Task<HttpResponseMessage> CreateOrderAsync(this HttpClient client, object order) =>
        client.PostAsJsonAsync(OrdersRoute, order, CancellationToken);

    public static object NewCustomer(string customerName) =>
        new
        {
            customerName,
            customerEmail = "petr@example.com",
            customerPhone = "+420 700 000 000",
        };

    public static Task<HttpResponseMessage> GetOrdersAsync(
        this HttpClient client,
        string? accessToken,
        string query = "") =>
        client.SendOrderRequestAsync(
            HttpMethod.Get,
            query.Length == 0 ? OrdersRoute : $"{OrdersRoute}?{query}",
            accessToken);

    public static Task<HttpResponseMessage> GetOrderAsync(this HttpClient client, Guid orderId, string? accessToken) =>
        client.SendOrderRequestAsync(HttpMethod.Get, $"{OrdersRoute}/{orderId}", accessToken);

    public static Task<HttpResponseMessage> CreateOrderItemAsync(
        this HttpClient client,
        Guid orderId,
        object item,
        string? accessToken) =>
        client.SendOrderRequestAsync(HttpMethod.Post, $"{OrdersRoute}/{orderId}/items", accessToken, item);

    public static Task<HttpResponseMessage> UpdateOrderCustomerAsync(
        this HttpClient client,
        Guid orderId,
        object customer,
        string? accessToken) =>
        client.SendOrderRequestAsync(HttpMethod.Put, $"{OrdersRoute}/{orderId}/customer", accessToken, customer);

    public static async Task<OrderResponse> ReadOrderAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no order.");

    public static async Task<OrdersPageResponse> ReadOrdersPageAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<OrdersPageResponse>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no orders page.");

    private static async Task<HttpResponseMessage> SendOrderRequestAsync(
        this HttpClient client,
        HttpMethod method,
        string route,
        string? accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, route);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}
