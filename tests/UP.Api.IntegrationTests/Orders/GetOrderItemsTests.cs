using System.Net;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class GetOrderItemsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Items_are_paged_by_start_time_with_total_of_this_order_only()
    {
        Product product = await SeedProductAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OrderResponse order = await CreateOrderAsync(
            OrdersApi.NewItem(product.Id, passengers: 3, startsAt: now.AddDays(3)),
            OrdersApi.NewItem(product.Id, passengers: 1, startsAt: now.AddDays(1)),
            OrdersApi.NewItem(product.Id, passengers: 2, startsAt: now.AddDays(2)));
        await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 9, startsAt: now.AddDays(1)));
        string accessToken = await LoginAsync();

        HttpResponseMessage response = await Client.GetOrderItemsAsync(order.Id, accessToken, "first=1&rows=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OrderItemsPageResponse page = await response.ReadOrderItemsPageAsync();
        page.TotalRecords.ShouldBe(3);
        page.Items.Select(item => item.Passengers).ShouldBe([2, 3]);
    }

    [Fact]
    public async Task Items_of_unknown_order_are_not_found()
    {
        HttpResponseMessage response = await Client.GetOrderItemsAsync(Guid.CreateVersion7(), await LoginAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Items_cannot_be_read_without_sign_in()
    {
        Product product = await SeedProductAsync();
        OrderResponse order = await CreateOrderAsync(OrdersApi.NewItem(product.Id));

        HttpResponseMessage response = await Client.GetOrderItemsAsync(order.Id, accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> LoginAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

    private async Task<OrderResponse> CreateOrderAsync(params object[] items)
    {
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(await LoginAsync(), items);
        response.EnsureSuccessStatusCode();
        return await response.ReadOrderAsync();
    }

    private async Task<Product> SeedProductAsync()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Type = ProductType.GroupExcursion,
            Name = "Excursion",
            SeoDescription = "Seeded for an order test",
            Attributes = "{}",
            Price = 40m,
        };
        await QueryDatabaseAsync(db =>
        {
            db.Products.Add(product);
            return db.SaveChangesAsync(CancellationToken);
        });
        return product;
    }
}
