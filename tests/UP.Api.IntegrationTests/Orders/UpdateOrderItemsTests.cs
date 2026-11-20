using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class UpdateOrderItemsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Updated_items_keep_their_ids_and_are_repriced_in_order_total()
    {
        Product excursion = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        Product transfer = await SeedProductAsync(ProductType.IndividualTransfer, 100m);
        OrderResponse created = await CreateOrderAsync(
            OrdersApi.NewItem(excursion.Id, passengers: 2),
            OrdersApi.NewItem(transfer.Id));
        Guid excursionItemId = created.Items.Single(item => item.ProductId == excursion.Id).Id;
        Guid transferItemId = created.Items.Single(item => item.ProductId == transfer.Id).Id;
        DateTimeOffset newStart = DateTimeOffset.UtcNow.AddDays(14);
        string accessToken = await LoginAsync();

        HttpResponseMessage response = await Client.UpdateOrderItemsAsync(
            created.Id,
            accessToken,
            ItemUpdate(excursionItemId, excursion.Id, passengers: 3, startsAt: newStart, notes: "Window seat"),
            ItemUpdate(transferItemId, excursion.Id, passengers: 1));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OrderResponse order = await response.ReadOrderAsync();
        order.TotalPrice.ShouldBe(160m);
        order.Items.Select(item => item.Id).ShouldBe([excursionItemId, transferItemId], ignoreOrder: true);
        OrderItemResponse switched = order.Items.Single(item => item.Id == transferItemId);
        switched.ProductId.ShouldBe(excursion.Id);
        switched.TotalPrice.ShouldBe(40m);
        Order stored = await ReadStoredOrderAsync(created.Id);
        stored.TotalPrice.ShouldBe(160m);
        OrderItem storedExcursion = stored.Items.Single(item => item.Id == excursionItemId);
        storedExcursion.TotalPrice.ShouldBe(120m);
        storedExcursion.Notes.ShouldBe("Window seat");
        storedExcursion.StartsAt.ShouldBe(newStart, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Batch_with_item_of_another_order_is_not_found_and_saves_nothing()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse target = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));
        OrderResponse other = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));
        string accessToken = await LoginAsync();

        HttpResponseMessage response = await Client.UpdateOrderItemsAsync(
            target.Id,
            accessToken,
            ItemUpdate(target.Items.Single().Id, product.Id, passengers: 4),
            ItemUpdate(other.Items.Single().Id, product.Id, passengers: 5));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        Order storedTarget = await ReadStoredOrderAsync(target.Id);
        storedTarget.Items.Single().Passengers.ShouldBe(2);
        storedTarget.TotalPrice.ShouldBe(80m);
        (await ReadStoredOrderAsync(other.Id)).Items.Single().Passengers.ShouldBe(2);
    }

    [Fact]
    public async Task Batch_listing_same_item_twice_is_refused_and_saves_nothing()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse created = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));
        Guid itemId = created.Items.Single().Id;
        string accessToken = await LoginAsync();

        HttpResponseMessage response = await Client.UpdateOrderItemsAsync(
            created.Id,
            accessToken,
            ItemUpdate(itemId, product.Id, passengers: 3),
            ItemUpdate(itemId, product.Id, passengers: 4));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadStoredOrderAsync(created.Id)).Items.Single().Passengers.ShouldBe(2);
    }

    [Fact]
    public async Task Items_cannot_be_updated_without_sign_in_and_stay_unchanged()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse created = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));

        HttpResponseMessage response = await Client.UpdateOrderItemsAsync(
            created.Id,
            accessToken: null,
            ItemUpdate(created.Items.Single().Id, product.Id, passengers: 5));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadStoredOrderAsync(created.Id)).Items.Single().Passengers.ShouldBe(2);
    }

    private static object ItemUpdate(
        Guid id,
        Guid productId,
        int passengers,
        DateTimeOffset? startsAt = null,
        string notes = "") =>
        new
        {
            id,
            productId,
            startsAt = startsAt ?? DateTimeOffset.UtcNow.AddDays(7),
            passengers,
            pickupAddress = "Grandhotel Pupp",
            notes,
        };

    private async Task<string> LoginAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

    private Task<Order> ReadStoredOrderAsync(Guid orderId) =>
        QueryDatabaseAsync(db => db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleAsync(order => order.Id == orderId, CancellationToken));

    private async Task<OrderResponse> CreateOrderAsync(params object[] items)
    {
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(await LoginAsync(), items);
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
