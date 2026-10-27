using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.Images;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.Images;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;

namespace UP.Api.IntegrationTests.Products;

public sealed class DeleteProductTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Several_products_are_deleted_in_one_request()
    {
        string accessToken = await SignInAsync();
        List<ProductResponse> created = await CreateProductsAsync(accessToken, 3);

        HttpResponseMessage response = await Client.DeleteProductsAsync(
            accessToken,
            created[0].Id,
            created[2].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await SavedIdsAsync()).ShouldBe([created[1].Id]);
    }

    [Fact]
    public async Task Same_id_twice_deletes_the_product_once()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = (await CreateProductsAsync(accessToken, 1))[0];

        HttpResponseMessage response = await Client.DeleteProductsAsync(accessToken, created.Id, created.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await SavedIdsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Deleting_a_product_deletes_its_images_and_files()
    {
        string accessToken = await SignInAsync();
        List<ProductResponse> created = await CreateProductsAsync(accessToken, 2);
        Guid deletedId = created[0].Id;
        Guid keptId = created[1].Id;
        await Client.CreateImagesAsync(accessToken, deletedId, ImagesApi.NewPhoto(40, 30), ImagesApi.NewPhoto(40, 30));
        await Client.CreateImagesAsync(accessToken, keptId, ImagesApi.NewPhoto(40, 30));

        HttpResponseMessage response = await Client.DeleteProductsAsync(accessToken, deletedId);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        List<ImageResponse> images = await (await Client.GetImagesAsync("Product", deletedId, keptId)).ReadImagesAsync();
        images.ShouldHaveSingleItem().EntityId.ShouldBe(keptId);
        (await CountStoredFilesAsync($"Product/{deletedId}/")).ShouldBe(0);
        (await CountStoredFilesAsync($"Product/{keptId}/")).ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_product_returns_404_and_deletes_nothing()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = (await CreateProductsAsync(accessToken, 1))[0];
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.DeleteProductsAsync(accessToken, created.Id, unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(unknownId.ToString());
        (await SavedIdsAsync()).ShouldBe([created.Id]);
    }

    [Fact]
    public async Task Empty_list_is_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.DeleteProductsAsync(accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deleting_products_without_sign_in_is_rejected()
    {
        string accessToken = await SignInAsync();
        ProductResponse created = (await CreateProductsAsync(accessToken, 1))[0];

        HttpResponseMessage response = await Client.DeleteProductsAsync(accessToken: null, created.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await SavedIdsAsync()).ShouldBe([created.Id]);
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

    private Task<List<Guid>> SavedIdsAsync() =>
        QueryDatabaseAsync(db => db.Products
            .Select(product => product.Id)
            .ToListAsync(CancellationToken));
}
