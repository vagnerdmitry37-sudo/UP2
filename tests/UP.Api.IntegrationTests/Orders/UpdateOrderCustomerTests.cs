using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class UpdateOrderCustomerTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Customer_is_changed_and_items_and_total_stay()
    {
        OrderResponse created = await CreateOrderAsync();
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.UpdateOrderCustomerAsync(
            created.Id,
            OrdersApi.NewCustomer("Petr Svoboda"),
            accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OrderResponse order = await response.ReadOrderAsync();
        order.CustomerName.ShouldBe("Petr Svoboda");
        order.Items.Single().Id.ShouldBe(created.Items.Single().Id);
        order.TotalPrice.ShouldBe(created.TotalPrice);
        Order stored = await ReadStoredOrderAsync(created.Id);
        stored.CustomerName.ShouldBe("Petr Svoboda");
        stored.CustomerEmail.ShouldBe("petr@example.com");
        stored.CustomerPhone.ShouldBe("+420 700 000 000");
    }

    [Fact]
    public async Task Customer_of_unknown_order_is_not_found()
    {
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.UpdateOrderCustomerAsync(
            Guid.CreateVersion7(),
            OrdersApi.NewCustomer("Petr Svoboda"),
            accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customer_cannot_be_changed_without_sign_in_and_stays_unchanged()
    {
        OrderResponse created = await CreateOrderAsync();

        HttpResponseMessage response = await Client.UpdateOrderCustomerAsync(
            created.Id,
            OrdersApi.NewCustomer("Petr Svoboda"),
            accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadStoredOrderAsync(created.Id)).CustomerName.ShouldBe("Anna Novak");
    }

    private async Task<OrderResponse> CreateOrderAsync()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Type = ProductType.GroupExcursion,
            Name = "Day trip to Bamberg",
            SeoDescription = "Seeded for an order test",
            Attributes = """{"durationMinutes":360,"meetingPoint":"Hotel Pupp"}""",
            Price = 40m,
        };
        await QueryDatabaseAsync(db =>
        {
            db.Products.Add(product);
            return db.SaveChangesAsync(CancellationToken);
        });
        HttpResponseMessage response = await Client.CreateOrderAsync(
            OrdersApi.NewOrder("Anna Novak", OrdersApi.NewItem(product.Id)));
        response.EnsureSuccessStatusCode();
        return await response.ReadOrderAsync();
    }

    private Task<Order> ReadStoredOrderAsync(Guid orderId) =>
        QueryDatabaseAsync(db => db.Orders
            .AsNoTracking()
            .SingleAsync(order => order.Id == orderId, CancellationToken));
}
