using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Customers;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Customers;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class GetOrderTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Order_is_returned_with_its_items()
    {
        Guid productId = await SeedProductAsync();
        OrderResponse created = await CreateOrderAsync(productId);
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.GetOrderAsync(created.Id, accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OrderResponse order = await response.ReadOrderAsync();
        order.Number.ShouldBe(created.Number);
        order.Items.Single().ProductId.ShouldBe(productId);
    }

    [Fact]
    public async Task Order_is_returned_with_its_customer()
    {
        OrderResponse created = await CreateOrderAsync(await SeedProductAsync());
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "Anna Novak",
            Email = "anna.novak@example.com",
            Phone = "+420 601 000 001",
            CreatedAt = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero),
        };
        await QueryDatabaseAsync(async db =>
        {
            db.Customers.Add(customer);
            Order order = await db.Orders.SingleAsync(order => order.Id == created.Id, CancellationToken);
            order.CustomerId = customer.Id;
            return await db.SaveChangesAsync(CancellationToken);
        });
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.GetOrderAsync(created.Id, accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        CustomerResponse? orderCustomer = (await response.ReadOrderAsync()).Customer;
        orderCustomer.ShouldNotBeNull();
        orderCustomer.Id.ShouldBe(customer.Id);
        orderCustomer.Name.ShouldBe(customer.Name);
        orderCustomer.Email.ShouldBe(customer.Email);
        orderCustomer.Phone.ShouldBe(customer.Phone);
    }

    [Fact]
    public async Task Unknown_order_is_not_found()
    {
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.GetOrderAsync(Guid.CreateVersion7(), accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Order_cannot_be_read_without_sign_in()
    {
        OrderResponse created = await CreateOrderAsync(await SeedProductAsync());

        HttpResponseMessage response = await Client.GetOrderAsync(created.Id, accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid productId)
    {
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(
            accessToken,
            OrdersApi.NewItem(productId));
        response.EnsureSuccessStatusCode();
        return await response.ReadOrderAsync();
    }

    private async Task<Guid> SeedProductAsync()
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
        return product.Id;
    }
}
