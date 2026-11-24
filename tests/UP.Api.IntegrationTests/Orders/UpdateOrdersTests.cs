using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.Customers;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;

namespace UP.Api.IntegrationTests.Orders;

public sealed class UpdateOrdersTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Orders_are_moved_to_new_customers_and_returned_in_request_order()
    {
        string accessToken = await SignInAsync();
        Guid firstOrderId = await CreateOrderIdAsync(accessToken);
        Guid secondOrderId = await CreateOrderIdAsync(accessToken);
        Guid newCustomerId = await Client.CreateCustomerIdAsync(accessToken);

        HttpResponseMessage response = await Client.UpdateOrdersAsync(
            accessToken,
            new { id = secondOrderId, customerId = newCustomerId },
            new { id = firstOrderId, customerId = newCustomerId });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        IReadOnlyList<OrderResponse> updated = await response.ReadOrdersAsync();
        updated.Select(order => order.Id).ShouldBe([secondOrderId, firstOrderId]);
        updated.ShouldAllBe(order => order.Customer!.Id == newCustomerId);
        (await ReadSavedCustomerIdAsync(firstOrderId)).ShouldBe(newCustomerId);
        (await ReadSavedCustomerIdAsync(secondOrderId)).ShouldBe(newCustomerId);
    }

    [Fact]
    public async Task Unknown_customer_is_refused_with_not_found_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid orderId = await CreateOrderIdAsync(accessToken);
        Guid? originalCustomerId = await ReadSavedCustomerIdAsync(orderId);

        HttpResponseMessage response = await Client.UpdateOrdersAsync(
            accessToken,
            new { id = orderId, customerId = Guid.CreateVersion7() });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadSavedCustomerIdAsync(orderId)).ShouldBe(originalCustomerId);
    }

    [Fact]
    public async Task Unknown_order_is_refused_with_not_found_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid orderId = await CreateOrderIdAsync(accessToken);
        Guid? originalCustomerId = await ReadSavedCustomerIdAsync(orderId);
        Guid newCustomerId = await Client.CreateCustomerIdAsync(accessToken);

        HttpResponseMessage response = await Client.UpdateOrdersAsync(
            accessToken,
            new { id = orderId, customerId = newCustomerId },
            new { id = Guid.CreateVersion7(), customerId = newCustomerId });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadSavedCustomerIdAsync(orderId)).ShouldBe(originalCustomerId);
    }

    [Fact]
    public async Task Updating_orders_without_sign_in_is_rejected_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid orderId = await CreateOrderIdAsync(accessToken);
        Guid? originalCustomerId = await ReadSavedCustomerIdAsync(orderId);
        Guid newCustomerId = await Client.CreateCustomerIdAsync(accessToken);

        HttpResponseMessage response = await Client.UpdateOrdersAsync(
            accessToken: null,
            new { id = orderId, customerId = newCustomerId });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadSavedCustomerIdAsync(orderId)).ShouldBe(originalCustomerId);
    }

    private async Task<Guid> CreateOrderIdAsync(string accessToken)
    {
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(accessToken);
        response.EnsureSuccessStatusCode();
        return (await response.ReadOrderAsync()).Id;
    }

    private Task<Guid?> ReadSavedCustomerIdAsync(Guid orderId) =>
        QueryDatabaseAsync(db => db.Orders
            .Where(order => order.Id == orderId)
            .Select(order => order.CustomerId)
            .SingleAsync(CancellationToken));

    private async Task<string> SignInAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;
}
