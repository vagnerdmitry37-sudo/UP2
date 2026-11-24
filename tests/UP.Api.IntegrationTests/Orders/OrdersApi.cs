using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using UP.Api.IntegrationTests.Customers;
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

    public static object NewOrder(Guid customerId, params object[] items) =>
        new
        {
            customerId,
            items,
        };

    public static Task<HttpResponseMessage> CreateOrderAsync(
        this HttpClient client,
        string? accessToken,
        object order) =>
        client.SendOrderRequestAsync(HttpMethod.Post, OrdersRoute, accessToken, order);

    public static Task<HttpResponseMessage> UpdateOrdersAsync(
        this HttpClient client,
        string? accessToken,
        params object[] orders) =>
        client.SendOrderRequestAsync(HttpMethod.Put, OrdersRoute, accessToken, orders);

    public static async Task<HttpResponseMessage> CreateOrderForNewCustomerAsync(
        this HttpClient client,
        string accessToken,
        params object[] items)
    {
        Guid customerId = await client.CreateCustomerIdAsync(accessToken);
        return await client.CreateOrderAsync(accessToken, NewOrder(customerId, items));
    }

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

    public static Task<HttpResponseMessage> GetOrdersByIdsAsync(
        this HttpClient client,
        string? accessToken,
        params Guid[] ids) =>
        client.SendOrderRequestAsync(
            HttpMethod.Get,
            $"{OrdersRoute}/by-ids?{string.Join("&", ids.Select(id => $"ids={id}"))}",
            accessToken);

    public static Task<HttpResponseMessage> DeleteOrdersAsync(
        this HttpClient client,
        string? accessToken,
        params Guid[] ids) =>
        client.SendOrderRequestAsync(
            HttpMethod.Delete,
            $"{OrdersRoute}?{string.Join("&", ids.Select(id => $"ids={id}"))}",
            accessToken);

    public static Task<HttpResponseMessage> CreateOrderItemAsync(
        this HttpClient client,
        Guid orderId,
        object item,
        string? accessToken) =>
        client.SendOrderRequestAsync(HttpMethod.Post, $"{OrdersRoute}/{orderId}/items", accessToken, item);

    public static Task<HttpResponseMessage> UpdateOrderItemsAsync(
        this HttpClient client,
        Guid orderId,
        string? accessToken,
        params object[] items) =>
        client.SendOrderRequestAsync(HttpMethod.Put, $"{OrdersRoute}/{orderId}/items", accessToken, items);

    public static Task<HttpResponseMessage> DeleteOrderItemsAsync(
        this HttpClient client,
        Guid orderId,
        string? accessToken,
        params Guid[] itemIds) =>
        client.SendOrderRequestAsync(
            HttpMethod.Delete,
            $"{OrdersRoute}/{orderId}/items?{string.Join("&", itemIds.Select(id => $"ids={id}"))}",
            accessToken);

    public static async Task<OrderResponse> ReadOrderAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no order.");

    public static async Task<IReadOnlyList<OrderResponse>> ReadOrdersAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<IReadOnlyList<OrderResponse>>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no orders.");

    public static async Task<OrderItemResponse> ReadOrderItemAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<OrderItemResponse>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no order item.");

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
