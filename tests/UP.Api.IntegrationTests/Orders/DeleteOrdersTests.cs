using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class DeleteOrdersTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Selected_orders_are_deleted_with_their_items_and_others_are_kept()
    {
        Guid productId = await SeedProductAsync();
        OrderResponse anna = await CreateOrderAsync(productId);
        OrderResponse petr = await CreateOrderAsync(productId);
        OrderResponse jana = await CreateOrderAsync(productId);
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.DeleteOrdersAsync(accessToken, anna.Id, jana.Id, jana.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await SavedOrderIdsAsync()).ShouldBe([petr.Id]);
        (await SavedItemOrderIdsAsync()).ShouldBe([petr.Id]);
    }

    [Fact]
    public async Task Unknown_id_returns_404_naming_it_and_deletes_nothing()
    {
        OrderResponse anna = await CreateOrderAsync(await SeedProductAsync());
        string accessToken = await SignInAsync();
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.DeleteOrdersAsync(accessToken, anna.Id, unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(unknownId.ToString());
        (await SavedOrderIdsAsync()).ShouldBe([anna.Id]);
    }

    [Fact]
    public async Task Orders_cannot_be_deleted_without_sign_in()
    {
        OrderResponse anna = await CreateOrderAsync(await SeedProductAsync());

        HttpResponseMessage response = await Client.DeleteOrdersAsync(accessToken: null, anna.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await SavedOrderIdsAsync()).ShouldBe([anna.Id]);
    }

    private Task<List<Guid>> SavedOrderIdsAsync() =>
        QueryDatabaseAsync(db => db.Orders.Select(order => order.Id).ToListAsync(CancellationToken));

    private Task<List<Guid>> SavedItemOrderIdsAsync() =>
        QueryDatabaseAsync(db => db.Set<OrderItem>().Select(item => item.OrderId).ToListAsync(CancellationToken));

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
