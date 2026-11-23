using System.Net;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class GetOrdersByIdsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Orders_are_returned_in_requested_order_with_their_items()
    {
        Guid productId = await SeedProductAsync();
        OrderResponse anna = await CreateOrderAsync(productId);
        OrderResponse petr = await CreateOrderAsync(productId);
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.GetOrdersByIdsAsync(accessToken, petr.Id, anna.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        IReadOnlyList<OrderResponse> orders = await response.ReadOrdersAsync();
        orders.Select(order => order.Id).ShouldBe([petr.Id, anna.Id]);
        orders.ShouldAllBe(order => order.Items.Single().ProductId == productId);
    }

    [Fact]
    public async Task Same_id_twice_returns_the_order_once()
    {
        OrderResponse anna = await CreateOrderAsync(await SeedProductAsync());
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.GetOrdersByIdsAsync(accessToken, anna.Id, anna.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadOrdersAsync()).ShouldHaveSingleItem().Id.ShouldBe(anna.Id);
    }

    [Fact]
    public async Task Unknown_id_returns_404_naming_it()
    {
        OrderResponse anna = await CreateOrderAsync(await SeedProductAsync());
        string accessToken = await SignInAsync();
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.GetOrdersByIdsAsync(accessToken, anna.Id, unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        string body = await response.Content.ReadAsStringAsync(CancellationToken);
        body.ShouldContain(unknownId.ToString());
        body.ShouldNotContain(anna.Id.ToString());
    }

    [Fact]
    public async Task More_than_one_hundred_ids_are_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.GetOrdersByIdsAsync(
            accessToken,
            [.. Enumerable.Range(0, 101).Select(_ => Guid.CreateVersion7())]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Orders_cannot_be_read_without_sign_in()
    {
        OrderResponse anna = await CreateOrderAsync(await SeedProductAsync());

        HttpResponseMessage response = await Client.GetOrdersByIdsAsync(accessToken: null, anna.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

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
