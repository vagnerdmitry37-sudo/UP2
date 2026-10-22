using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Products;

public sealed class UpdateProductTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Updated_product_is_returned()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = await CreateProductAsync(accessToken);

        HttpResponseMessage response = await Client.UpdateProductAsync(
            accessToken,
            created.Id,
            ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel", price: 40m, attributes: new { from = "Airport" }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProductResponse product = await response.ReadProductAsync();
        product.Id.ShouldBe(created.Id);
        product.Type.ShouldBe(ProductType.Transfer);
        product.Name.ShouldBe("Airport to hotel");
        product.Price.ShouldBe(40m);
        product.Attributes.GetProperty("from").GetString().ShouldBe("Airport");
    }

    [Fact]
    public async Task Updated_product_is_saved()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = await CreateProductAsync(accessToken);

        await Client.UpdateProductAsync(accessToken, created.Id, ProductsApi.NewProduct(name: "Night Walk"));

        string savedName = await QueryDatabaseAsync(db => db.Products
            .Where(product => product.Id == created.Id)
            .Select(product => product.Name)
            .SingleAsync(CancellationToken));
        savedName.ShouldBe("Night Walk");
    }

    [Fact]
    public async Task Updating_unknown_product_returns_404()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.UpdateProductAsync(
            accessToken,
            Guid.CreateVersion7(),
            ProductsApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Updating_product_without_sign_in_is_rejected()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = await CreateProductAsync(accessToken);

        HttpResponseMessage response = await Client.UpdateProductAsync(accessToken: null, created.Id, ProductsApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }

    private async Task<ProductResponse> CreateProductAsync(string accessToken) =>
        await (await Client.CreateProductAsync(accessToken, ProductsApi.NewProduct())).ReadProductAsync();
}
