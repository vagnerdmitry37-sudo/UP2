using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.Orders;
using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Customers;

public sealed class DeleteCustomersTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "manager@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Several_customers_are_deleted_in_one_request()
    {
        string accessToken = await SignInAsync();
        Guid firstId = await Client.CreateCustomerIdAsync(accessToken);
        Guid keptId = await Client.CreateCustomerIdAsync(accessToken);
        Guid thirdId = await Client.CreateCustomerIdAsync(accessToken);

        HttpResponseMessage response = await Client.DeleteCustomersAsync(accessToken, firstId, thirdId);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await SavedIdsAsync()).ShouldBe([keptId]);
    }

    [Fact]
    public async Task Customer_with_orders_is_refused_with_conflict_and_deletes_nothing()
    {
        string accessToken = await SignInAsync();
        Guid customerWithOrderId = await Client.CreateCustomerIdAsync(accessToken);
        Guid customerWithoutOrdersId = await Client.CreateCustomerIdAsync(accessToken);
        (await Client.CreateOrderAsync(accessToken, OrdersApi.NewOrder(customerWithOrderId)))
            .EnsureSuccessStatusCode();

        HttpResponseMessage response = await Client.DeleteCustomersAsync(
            accessToken,
            customerWithOrderId,
            customerWithoutOrdersId);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(customerWithOrderId.ToString());
        (await SavedIdsAsync()).ShouldBe([customerWithOrderId, customerWithoutOrdersId], ignoreOrder: true);
    }

    [Fact]
    public async Task Unknown_customer_returns_404_and_deletes_nothing()
    {
        string accessToken = await SignInAsync();
        Guid customerId = await Client.CreateCustomerIdAsync(accessToken);
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.DeleteCustomersAsync(accessToken, customerId, unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(unknownId.ToString());
        (await SavedIdsAsync()).ShouldBe([customerId]);
    }

    [Fact]
    public async Task Deleting_customers_without_sign_in_is_rejected_and_deletes_nothing()
    {
        Guid customerId = await Client.CreateCustomerIdAsync(await SignInAsync());

        HttpResponseMessage response = await Client.DeleteCustomersAsync(accessToken: null, customerId);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await SavedIdsAsync()).ShouldBe([customerId]);
    }

    private async Task<string> SignInAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

    private Task<List<Guid>> SavedIdsAsync() =>
        QueryDatabaseAsync(db => db.Customers
            .Select(customer => customer.Id)
            .ToListAsync(CancellationToken));
}
