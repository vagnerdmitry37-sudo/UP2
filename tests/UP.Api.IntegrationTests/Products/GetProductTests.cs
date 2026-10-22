using System.Net;

using UP.Api.Auth;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Products;

public sealed class GetProductTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Product_is_returned_by_id_without_sign_in()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        ProductResponse created = await (await Client.CreateProductAsync(token.AccessToken, ProductsApi.NewProduct()))
            .ReadProductAsync();

        HttpResponseMessage response = await Client.GetProductAsync(created.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProductResponse product = await response.ReadProductAsync();
        product.Id.ShouldBe(created.Id);
        product.Type.ShouldBe(ProductType.Excursion);
        product.Name.ShouldBe("Old Town Walk");
        product.Price.ShouldBe(25.50m);
        product.Attributes.GetProperty("durationMinutes").GetInt32().ShouldBe(180);
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        HttpResponseMessage response = await Client.GetProductAsync(Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
