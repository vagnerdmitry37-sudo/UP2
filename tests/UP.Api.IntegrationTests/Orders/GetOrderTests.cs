using System.Net;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
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
        order.CustomerName.ShouldBe("Anna Novak");
        order.Items.Single().ProductId.ShouldBe(productId);
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
        HttpResponseMessage response = await Client.CreateOrderAsync(
            OrdersApi.NewOrder("Anna Novak", OrdersApi.NewItem(productId)));
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
