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
    public async Task Created_product_is_returned_with_its_location()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductAsync(accessToken, ProductsApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        ProductResponse product = await response.ReadProductAsync();
        product.Id.ShouldNotBe(Guid.Empty);
        product.Type.ShouldBe(ProductType.Excursion);
        product.Name.ShouldBe("Old Town Walk");
        product.Description.ShouldBe("Three hours through the historic centre");
        product.Price.ShouldBe(25.50m);
        product.Attributes.GetProperty("durationMinutes").GetInt32().ShouldBe(180);
        product.Attributes.GetProperty("meetingPoint").GetString().ShouldBe("Main square");
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldBe($"/{ProductsApi.ProductsRoute}/{product.Id}");
    }

    [Fact]
    public async Task Created_product_is_saved()
    {
        string accessToken = await SignInAsync();

        ProductResponse created = await (await Client.CreateProductAsync(accessToken, ProductsApi.NewProduct()))
            .ReadProductAsync();

        bool saved = await QueryDatabaseAsync(db => db.Products.AnyAsync(product => product.Id == created.Id, CancellationToken));
        saved.ShouldBeTrue();
    }

    [Fact]
    public async Task Transfer_can_be_created()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateProductAsync(
            accessToken,
            ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel", attributes: new { from = "Airport", to = "Hotel" }));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.ReadProductAsync()).Type.ShouldBe(ProductType.Transfer);
    }

    [Fact]
    public async Task Creating_product_without_sign_in_is_rejected()
    {
        HttpResponseMessage response = await Client.CreateProductAsync(accessToken: null, ProductsApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }
}
