using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.Customers;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class CreateOrderTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";
    private const string ExcursionAttributes = """{"durationMinutes":360,"meetingPoint":"Hotel Pupp"}""";

    [Fact]
    public async Task Order_is_priced_per_person_or_per_booking_by_product_type_and_keeps_product_snapshot()
    {
        Product excursion = await SeedProductAsync(ProductType.GroupExcursion, 40m, ExcursionAttributes);
        Product transfer = await SeedProductAsync(
            ProductType.IndividualTransfer,
            100m,
            """{"from":"Airport","to":"Hotel","maxPassengers":4}""");

        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(
            await SignInAsync(),
            OrdersApi.NewItem(excursion.Id, passengers: 3),
            OrdersApi.NewItem(transfer.Id, passengers: 3));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        OrderResponse order = await response.ReadOrderAsync();
        order.Status.ShouldBe(OrderStatus.Pending);
        order.Number.ShouldMatch(@"^ORD-\d{4}-\d{6}$");
        order.Items.Select(item => item.TotalPrice).ShouldBe([120m, 100m]);
        order.TotalPrice.ShouldBe(220m);
        order.Items[0].ProductName.ShouldBe(excursion.Name);
        order.Items[0].UnitPrice.ShouldBe(40m);
        order.Items[1].ProductAttributes.GetProperty("maxPassengers").GetInt32().ShouldBe(4);
    }

    [Fact]
    public async Task Order_for_customer_is_created_without_items_and_belongs_to_that_customer()
    {
        string accessToken = await SignInAsync();
        Guid customerId = await Client.CreateCustomerIdAsync(accessToken);

        HttpResponseMessage response = await Client.CreateOrderAsync(accessToken, OrdersApi.NewOrder(customerId));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        OrderResponse order = await response.ReadOrderAsync();
        order.Customer.ShouldNotBeNull().Id.ShouldBe(customerId);
        order.Items.ShouldBeEmpty();
        order.TotalPrice.ShouldBe(0m);
        Guid? savedCustomerId = await QueryDatabaseAsync(db => db.Orders
            .Where(saved => saved.Id == order.Id)
            .Select(saved => saved.CustomerId)
            .SingleAsync(CancellationToken));
        savedCustomerId.ShouldBe(customerId);
    }

    [Fact]
    public async Task Each_order_gets_its_own_number()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m, ExcursionAttributes);

        OrderResponse first = await CreateOrderAsync(product.Id);
        OrderResponse second = await CreateOrderAsync(product.Id);

        second.Number.ShouldNotBe(first.Number);
    }

    [Fact]
    public async Task Unknown_customer_is_refused_with_not_found_and_saves_nothing()
    {
        HttpResponseMessage response = await Client.CreateOrderAsync(
            await SignInAsync(),
            OrdersApi.NewOrder(Guid.CreateVersion7()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CountOrdersAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Creating_order_without_sign_in_is_rejected_and_saves_nothing()
    {
        Guid customerId = await Client.CreateCustomerIdAsync(await SignInAsync());

        HttpResponseMessage response = await Client.CreateOrderAsync(
            accessToken: null,
            OrdersApi.NewOrder(customerId));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await CountOrdersAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Start_in_the_past_is_refused_and_saves_nothing()
    {
        Product product = await SeedProductAsync(ProductType.GroupExcursion, 40m, ExcursionAttributes);

        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(
            await SignInAsync(),
            OrdersApi.NewItem(product.Id, startsAt: DateTimeOffset.UtcNow.AddHours(-1)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CountOrdersAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Unknown_product_is_refused_with_not_found_and_saves_nothing()
    {
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(
            await SignInAsync(),
            OrdersApi.NewItem(Guid.CreateVersion7()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CountOrdersAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task More_passengers_than_product_allows_are_refused_and_save_nothing()
    {
        Product product = await SeedProductAsync(
            ProductType.IndividualExcursion,
            300m,
            """{"durationMinutes":240,"maxPassengers":4}""");

        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(
            await SignInAsync(),
            OrdersApi.NewItem(product.Id, passengers: 5));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CountOrdersAsync()).ShouldBe(0);
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid productId)
    {
        HttpResponseMessage response = await Client.CreateOrderForNewCustomerAsync(
            await SignInAsync(),
            OrdersApi.NewItem(productId));
        response.EnsureSuccessStatusCode();
        return await response.ReadOrderAsync();
    }

    private async Task<string> SignInAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

    private async Task<Product> SeedProductAsync(ProductType type, decimal price, string attributes)
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Name = $"{type} product",
            SeoDescription = "Seeded for an order test",
            Attributes = attributes,
            Price = price,
        };
        await QueryDatabaseAsync(db =>
        {
            db.Products.Add(product);
            return db.SaveChangesAsync(CancellationToken);
        });
        return product;
    }

    private Task<int> CountOrdersAsync() =>
        QueryDatabaseAsync(db => db.Orders.CountAsync(CancellationToken));
}
