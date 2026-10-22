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
        ProductResponse created = (await CreateProductsAsync(accessToken, 1))[0];

        HttpResponseMessage response = await Client.UpdateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(
                id: created.Id,
                type: "Transfer",
                name: "Airport to hotel",
                price: 40m,
                attributes: new { from = "Airport" }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProductResponse product = (await response.ReadProductsAsync()).ShouldHaveSingleItem();
        product.Id.ShouldBe(created.Id);
        product.Type.ShouldBe(ProductType.Transfer);
        product.Name.ShouldBe("Airport to hotel");
        product.Price.ShouldBe(40m);
        product.Attributes.GetProperty("from").GetString().ShouldBe("Airport");
    }

    [Fact]
    public async Task Several_products_are_updated_in_one_request()
    {
        string accessToken = await SignInAsync();
        List<ProductResponse> created = await CreateProductsAsync(accessToken, 2);

        HttpResponseMessage response = await Client.UpdateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(id: created[0].Id, name: "Night Walk"),
            ProductsApi.NewProduct(id: created[1].Id, name: "Food Tour"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await SavedNamesAsync()).ShouldBe(["Night Walk", "Food Tour"], ignoreOrder: true);
    }

    [Fact]
    public async Task One_invalid_product_rejects_the_whole_request_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        List<ProductResponse> created = await CreateProductsAsync(accessToken, 2);

        HttpResponseMessage response = await Client.UpdateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(id: created[0].Id, name: "Night Walk"),
            ProductsApi.NewProduct(id: created[1].Id, name: ""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain("[1].Name");
        (await SavedNamesAsync()).ShouldBe(["Old Town Walk", "Old Town Walk"]);
    }

    [Fact]
    public async Task Unknown_product_returns_404_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        List<ProductResponse> created = await CreateProductsAsync(accessToken, 1);
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.UpdateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(id: created[0].Id, name: "Night Walk"),
            ProductsApi.NewProduct(id: unknownId));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(unknownId.ToString());
        (await SavedNamesAsync()).ShouldBe(["Old Town Walk"]);
    }

    [Fact]
    public async Task Same_product_twice_is_rejected()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = (await CreateProductsAsync(accessToken, 1))[0];

        HttpResponseMessage response = await Client.UpdateProductsAsync(
            accessToken,
            ProductsApi.NewProduct(id: created.Id, name: "Night Walk"),
            ProductsApi.NewProduct(id: created.Id, name: "Food Tour"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SavedNamesAsync()).ShouldBe(["Old Town Walk"]);
    }

    [Fact]
    public async Task Empty_list_is_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.UpdateProductsAsync(accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Updating_products_without_sign_in_is_rejected()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = (await CreateProductsAsync(accessToken, 1))[0];

        HttpResponseMessage response = await Client.UpdateProductsAsync(
            accessToken: null,
            ProductsApi.NewProduct(id: created.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }

    private async Task<List<ProductResponse>> CreateProductsAsync(string accessToken, int count) =>
        await (await Client.CreateProductsAsync(
            accessToken,
            [.. Enumerable.Range(0, count).Select(_ => ProductsApi.NewProduct())])).ReadProductsAsync();

    private Task<List<string>> SavedNamesAsync() =>
        QueryDatabaseAsync(db => db.Products
            .OrderBy(product => product.Id)
            .Select(product => product.Name)
            .ToListAsync(CancellationToken));
}
