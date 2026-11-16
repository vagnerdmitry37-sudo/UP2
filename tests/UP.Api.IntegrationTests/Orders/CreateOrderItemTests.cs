using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class CreateOrderItemTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Added_item_is_priced_and_counted_in_order_total()
    {
        Product excursion = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        Product transfer = await SeedProductAsync(ProductType.IndividualTransfer, 100m);
        OrderResponse created = await CreateOrderAsync(excursion.Id);
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.CreateOrderItemAsync(
            created.Id,
            OrdersApi.NewItem(transfer.Id, passengers: 3),
            accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        OrderResponse order = await response.ReadOrderAsync();
        order.Items.Select(item => item.TotalPrice).ShouldBe([80m, 100m], ignoreOrder: true);
        order.TotalPrice.ShouldBe(180m);
        Order stored = await QueryDatabaseAsync(db => db.Orders
            .AsNoTracking()
            .Include(storedOrder => storedOrder.Items)
            .SingleAsync(storedOrder => storedOrder.Id == created.Id, CancellationToken));
        stored.TotalPrice.ShouldBe(180m);
        stored.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Item_for_unknown_order_is_not_found()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.CreateOrderItemAsync(
            Guid.CreateVersion7(),
            OrdersApi.NewItem(product.Id),
            accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Item_cannot_be_added_without_sign_in_and_saves_nothing()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse created = await CreateOrderAsync(product.Id);

        HttpResponseMessage response = await Client.CreateOrderItemAsync(
            created.Id,
            OrdersApi.NewItem(product.Id),
            accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await QueryDatabaseAsync(db => db.Set<OrderItem>().CountAsync(CancellationToken))).ShouldBe(1);
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid productId)
    {
        HttpResponseMessage response = await Client.CreateOrderAsync(
            OrdersApi.NewOrder("Anna Novak", OrdersApi.NewItem(productId)));
        response.EnsureSuccessStatusCode();
        return await response.ReadOrderAsync();
    }

    private async Task<Product> SeedProductAsync(ProductType type, decimal price)
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Name = $"{type} product",
            SeoDescription = "Seeded for an order test",
            Attributes = "{}",
            Price = price,
        };
        await QueryDatabaseAsync(db =>
        {
            db.Products.Add(product);
            return db.SaveChangesAsync(CancellationToken);
        });
        return product;
    }
}
