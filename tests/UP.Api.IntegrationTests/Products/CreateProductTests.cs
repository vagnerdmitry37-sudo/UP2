using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Products;

public sealed class CreateProductTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Created_product_is_returned()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(accessToken, ProductsApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        ProductResponse product = (await response.ReadProductsAsync()).ShouldHaveSingleItem();
        product.Id.ShouldNotBe(Guid.Empty);
        product.Type.ShouldBe(ProductType.Excursion);
        product.Name.ShouldBe("Old Town Walk");
        product.Description.ShouldBe("Three hours through the historic centre");
        product.Price.ShouldBe(25.50m);
        product.Attributes.GetProperty("durationMinutes").GetInt32().ShouldBe(180);
        product.Attributes.GetProperty("meetingPoint").GetString().ShouldBe("Main square");
    }

    [Fact]
    public async Task Several_products_are_created_in_one_request()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(name: "Old Town Walk"),
            ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        List<ProductResponse> products = await response.ReadProductsAsync();
        products.Select(product => product.Name).ShouldBe(["Old Town Walk", "Airport to hotel"]);
        products.Select(product => product.Id).Distinct().Count().ShouldBe(2);
        int savedCount = await QueryDatabaseAsync(db => db.Products.CountAsync(CancellationToken));
        savedCount.ShouldBe(2);
    }

    [Fact]
    public async Task Created_product_is_saved()
    {
        string accessToken = await SignInAsync();

        ProductResponse created = (await (await Client.CreateProductsAsync(accessToken, ProductsApi.NewProduct()))
            .ReadProductsAsync()).ShouldHaveSingleItem();

        bool saved = await QueryDatabaseAsync(db => db.Products.AnyAsync(product => product.Id == created.Id, CancellationToken));
        saved.ShouldBeTrue();
    }

    [Fact]
    public async Task Transfer_can_be_created()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel", attributes: new { from = "Airport", to = "Hotel" }));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.ReadProductsAsync()).ShouldHaveSingleItem().Type.ShouldBe(ProductType.Transfer);
    }

    [Fact]
    public async Task One_invalid_product_rejects_the_whole_request_and_saves_nothing()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(name: "Old Town Walk"),
            ProductsApi.NewProduct(name: ""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain("[1].Name");
        int savedCount = await QueryDatabaseAsync(db => db.Products.CountAsync(CancellationToken));
        savedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Negative_price_is_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(accessToken, ProductsApi.NewProduct(price: -1m));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Empty_list_is_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task More_than_one_hundred_products_are_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(
            accessToken,
            [.. Enumerable.Range(0, 101).Select(_ => ProductsApi.NewProduct())]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Price_sent_as_string_is_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductsAsync(
            accessToken,
            new
            {
                type = "Excursion",
                name = "Old Town Walk",
                description = "Three hours through the historic centre",
                price = "25.50",
                attributes = new { durationMinutes = 180 },
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_products_without_sign_in_is_rejected()
    {
        HttpResponseMessage response = await Client.CreateProductsAsync(accessToken: null, ProductsApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }
}
