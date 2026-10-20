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
        await Client.CreateProductAsync(token.AccessToken, ProductsApi.NewProduct(name: "Old Town Walk"));
        await Client.CreateProductAsync(token.AccessToken, ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel"));

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
}
