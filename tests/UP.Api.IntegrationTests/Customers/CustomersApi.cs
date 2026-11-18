using System.Net.Http.Headers;
using System.Net.Http.Json;

using UP.Api.Customers;

namespace UP.Api.IntegrationTests.Customers;

internal static class CustomersApi
{
    public const string CustomersRoute = "api/customers";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static object NewCustomer(
        string name = "Anna Smith",
        string email = "anna@example.com",
        string phone = "+44 20 7946 0000") =>
        new { name, email, phone };

    public static async Task<HttpResponseMessage> CreateCustomerAsync(
        this HttpClient client,
        string? accessToken,
        object customer)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, CustomersRoute)
        {
            Content = JsonContent.Create(customer),
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<HttpResponseMessage> GetCustomersAsync(
        this HttpClient client,
        string? accessToken,
        string query = "")
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            query.Length == 0 ? CustomersRoute : $"{CustomersRoute}?{query}");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<Guid> CreateCustomerIdAsync(this HttpClient client, string accessToken)
    {
        HttpResponseMessage response = await client.CreateCustomerAsync(
            accessToken,
            NewCustomer(email: $"customer-{Guid.NewGuid():N}@example.com"));
        response.EnsureSuccessStatusCode();
        return (await response.ReadCustomerAsync()).Id;
    }

    public static async Task<CustomersPageResponse> ReadCustomersPageAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<CustomersPageResponse>(CancellationToken)
        ?? throw new InvalidOperationException("Response has no customers page.");

    public static async Task<CustomerResponse> ReadCustomerAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<CustomerResponse>(CancellationToken)
        ?? throw new InvalidOperationException("Response has no customer.");
}
