using System.Net;

using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Orders;

public sealed class GetOrdersTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Orders_cannot_be_listed_without_sign_in()
    {
        HttpResponseMessage response = await Client.GetOrdersAsync(accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Orders_are_paged_newest_first_with_their_items_and_total_of_all_orders()
    {
        Guid productId = await SeedProductAsync();
        foreach (string customer in new[] { "First Customer", "Second Customer", "Third Customer" })
        {
            HttpResponseMessage created = await Client.CreateOrderAsync(
                OrdersApi.NewOrder(customer, OrdersApi.NewItem(productId)));
            created.EnsureSuccessStatusCode();
        }
        string accessToken = (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

        OrdersPageResponse firstPage = await ReadPageAsync(accessToken, "first=0&rows=2");
        OrdersPageResponse secondPage = await ReadPageAsync(accessToken, "first=2&rows=2");

        firstPage.Items.Select(order => order.CustomerName).ShouldBe(["Third Customer", "Second Customer"]);
        secondPage.Items.Select(order => order.CustomerName).ShouldBe(["First Customer"]);
        firstPage.TotalRecords.ShouldBe(3);
        firstPage.Items[0].Items.Single().ProductId.ShouldBe(productId);
    }

    private async Task<OrdersPageResponse> ReadPageAsync(string accessToken, string query)
    {
        HttpResponseMessage response = await Client.GetOrdersAsync(accessToken, query);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ReadOrdersPageAsync();
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
