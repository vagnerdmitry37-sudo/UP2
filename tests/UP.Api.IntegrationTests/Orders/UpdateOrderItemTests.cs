using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class UpdateOrderItemTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Updated_item_keeps_its_id_and_is_repriced_in_order_total()
    {
        Product excursion = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        Product transfer = await SeedProductAsync(ProductType.IndividualTransfer, 100m);
        OrderResponse created = await CreateOrderAsync(
            OrdersApi.NewItem(excursion.Id, passengers: 2),
            OrdersApi.NewItem(transfer.Id));
        OrderItemResponse edited = created.Items.Single(item => item.ProductId == excursion.Id);
        DateTimeOffset newStart = DateTimeOffset.UtcNow.AddDays(14);
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.UpdateOrderItemAsync(
            created.Id,
            edited.Id,
            new
            {
                productId = excursion.Id,
                startsAt = newStart,
                passengers = 3,
                pickupAddress = "Grandhotel Pupp",
                notes = "Window seat",
            },
            accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        OrderResponse order = await response.ReadOrderAsync();
        order.TotalPrice.ShouldBe(220m);
        OrderItemResponse updated = order.Items.Single(item => item.Id == edited.Id);
        updated.Passengers.ShouldBe(3);
        updated.TotalPrice.ShouldBe(120m);
        updated.PickupAddress.ShouldBe("Grandhotel Pupp");
        updated.Notes.ShouldBe("Window seat");
        Order stored = await ReadStoredOrderAsync(created.Id);
        stored.TotalPrice.ShouldBe(220m);
        stored.Items.Count.ShouldBe(2);
        stored.Items.Single(item => item.Id == edited.Id).StartsAt.ShouldBe(newStart, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Item_of_another_order_is_not_found_and_stays_unchanged()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse target = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));
        OrderResponse other = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));
        Guid otherItemId = other.Items.Single().Id;
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        HttpResponseMessage response = await Client.UpdateOrderItemAsync(
            target.Id,
            otherItemId,
            OrdersApi.NewItem(product.Id, passengers: 5),
            accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadStoredOrderAsync(other.Id)).Items.Single().Passengers.ShouldBe(2);
        (await ReadStoredOrderAsync(target.Id)).TotalPrice.ShouldBe(80m);
    }

    [Fact]
    public async Task Item_cannot_be_updated_without_sign_in_and_stays_unchanged()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m);
        OrderResponse created = await CreateOrderAsync(OrdersApi.NewItem(product.Id, passengers: 2));

        HttpResponseMessage response = await Client.UpdateOrderItemAsync(
            created.Id,
            created.Items.Single().Id,
            OrdersApi.NewItem(product.Id, passengers: 5),
            accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadStoredOrderAsync(created.Id)).Items.Single().Passengers.ShouldBe(2);
    }

    private Task<Order> ReadStoredOrderAsync(Guid orderId) =>
        QueryDatabaseAsync(db => db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleAsync(order => order.Id == orderId, CancellationToken));

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
