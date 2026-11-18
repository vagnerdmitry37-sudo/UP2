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

    public static async Task<CustomerResponse> ReadCustomerAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<CustomerResponse>(CancellationToken)
        ?? throw new InvalidOperationException("Response has no customer.");
}
