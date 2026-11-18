using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class DeleteOrderItemsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Selected_items_are_deleted_and_order_total_is_recalculated()
    {
        Product excursion = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        Product transfer = await SeedProductAsync(ProductType.IndividualTransfer, 100m);
        OrderResponse created = await CreateOrderAsync(
            OrdersApi.NewItem(excursion.Id, passengers: 1),
            OrdersApi.NewItem(excursion.Id, passengers: 2),
            OrdersApi.NewItem(transfer.Id));
        Guid[] excursionItemIds = [.. created.Items
            .Where(item => item.ProductId == excursion.Id)
            .Select(item => item.Id)];
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.DeleteOrderItemsAsync(
            created.Id,
            accessToken,
            [.. excursionItemIds, excursionItemIds[0]]);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OrderResponse order = await response.ReadOrderAsync();
        order.Items.Select(item => item.ProductId).ShouldBe([transfer.Id]);
        order.TotalPrice.ShouldBe(100m);
        Order stored = await StoredOrderAsync(created.Id);
        stored.Items.Select(item => item.ProductId).ShouldBe([transfer.Id]);
        stored.TotalPrice.ShouldBe(100m);
    }

    [Fact]
    public async Task Item_of_another_order_returns_404_naming_it_and_deletes_nothing()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse anna = await CreateOrderAsync(OrdersApi.NewItem(product.Id));
        OrderResponse petr = await CreateOrderAsync(OrdersApi.NewItem(product.Id));
        Guid petrItemId = petr.Items.Single().Id;
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.DeleteOrderItemsAsync(
            anna.Id,
            accessToken,
            anna.Items.Single().Id,
            petrItemId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(petrItemId.ToString());
        (await QueryDatabaseAsync(db => db.Set<OrderItem>().CountAsync(CancellationToken))).ShouldBe(2);
        (await StoredOrderAsync(anna.Id)).TotalPrice.ShouldBe(80m);
    }

    [Fact]
    public async Task Items_cannot_be_deleted_without_sign_in()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse created = await CreateOrderAsync(OrdersApi.NewItem(product.Id));

        HttpResponseMessage response = await Client.DeleteOrderItemsAsync(
            created.Id,
            accessToken: null,
            created.Items.Single().Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await QueryDatabaseAsync(db => db.Set<OrderItem>().CountAsync(CancellationToken))).ShouldBe(1);
    }

    private Task<Order> StoredOrderAsync(Guid orderId) =>
        QueryDatabaseAsync(db => db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleAsync(order => order.Id == orderId, CancellationToken));

    private async Task<string> SignInAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

    private async Task<OrderResponse> CreateOrderAsync(params object[] items)
    {
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(accessToken, items);
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
