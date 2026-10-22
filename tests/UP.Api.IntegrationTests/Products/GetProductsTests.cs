using System.Net;

using UP.Api.Auth;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Products;

public sealed class GetProductsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Products_list_is_empty_when_nothing_was_created()
    {
        HttpResponseMessage response = await Client.GetProductsAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadProductsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Created_products_are_listed_in_creation_order()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        await Client.CreateProductsAsync(token.AccessToken, ProductsApi.NewProduct(name: "Old Town Walk"));
        await Client.CreateProductsAsync(token.AccessToken, ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel"));

        HttpResponseMessage response = await Client.GetProductsAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<ProductResponse> products = await response.ReadProductsAsync();
        products.Select(product => product.Name).ShouldBe(["Old Town Walk", "Airport to hotel"]);
        products[1].Type.ShouldBe(ProductType.Transfer);
        products[0].Attributes.GetProperty("durationMinutes").GetInt32().ShouldBe(180);
    }

    [Fact]
    public async Task Products_can_be_listed_without_sign_in()
    {
        HttpResponseMessage response = await Client.GetProductsAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Requested_products_are_returned_in_requested_order()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        ProductResponse walk = await CreateProductAsync(token.AccessToken, "Old Town Walk");
        await CreateProductAsync(token.AccessToken, "Food Tour");
        ProductResponse transfer = await CreateProductAsync(token.AccessToken, "Airport to hotel");

        HttpResponseMessage response = await Client.GetProductsAsync(transfer.Id, walk.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadProductsAsync()).Select(product => product.Name).ShouldBe(["Airport to hotel", "Old Town Walk"]);
    }

    [Fact]
    public async Task Same_id_twice_returns_the_product_once()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        ProductResponse walk = await CreateProductAsync(token.AccessToken, "Old Town Walk");

        HttpResponseMessage response = await Client.GetProductsAsync(walk.Id, walk.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadProductsAsync()).ShouldHaveSingleItem().Id.ShouldBe(walk.Id);
    }

    [Fact]
    public async Task Unknown_id_returns_404_naming_it()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        ProductResponse walk = await CreateProductAsync(token.AccessToken, "Old Town Walk");
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.GetProductsAsync(walk.Id, unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(unknownId.ToString());
    }

    [Fact]
    public async Task More_than_one_hundred_ids_are_rejected()
    {
        HttpResponseMessage response = await Client.GetProductsAsync(
            [.. Enumerable.Range(0, 101).Select(_ => Guid.CreateVersion7())]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<ProductResponse> CreateProductAsync(string accessToken, string name) =>
        (await (await Client.CreateProductsAsync(accessToken, ProductsApi.NewProduct(name: name)))
            .ReadProductsAsync()).ShouldHaveSingleItem();
}
