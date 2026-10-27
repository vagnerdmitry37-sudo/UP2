using System.Net;

using UP.Api.Auth;
using UP.Api.Images;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Images;

public sealed class DeleteImagesTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Deleted_image_is_removed_from_the_list_and_from_storage()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 2);

        HttpResponseMessage response = await Client.DeleteImagesAsync(accessToken, uploaded[0].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        listed.Select(image => image.Id).ShouldBe([uploaded[1].Id]);
        (await ReadStoredFileAsync(KeyOf(uploaded[0]))).ShouldBeNull();
        (await ReadStoredFileAsync(KeyOf(uploaded[1]))).ShouldNotBeNull();
    }

    [Fact]
    public async Task Several_images_are_deleted_in_one_request()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 3);

        HttpResponseMessage response = await Client.DeleteImagesAsync(accessToken, uploaded[0].Id, uploaded[2].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        listed.Select(image => image.Id).ShouldBe([uploaded[1].Id]);
        (await CountStoredFilesAsync($"Product/{productId}/")).ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_id_deletes_nothing_and_returns_404()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 1);

        HttpResponseMessage response = await Client.DeleteImagesAsync(accessToken, uploaded[0].Id, Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        listed.ShouldHaveSingleItem();
        (await ReadStoredFileAsync(KeyOf(uploaded[0]))).ShouldNotBeNull();
    }

    [Fact]
    public async Task Request_without_ids_is_rejected()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.DeleteImagesAsync(accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_requires_sign_in()
    {
        HttpResponseMessage response = await Client.DeleteImagesAsync(null, Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string KeyOf(ImageResponse image) => image.Url[(ApiFactory.ImagesPublicBaseUrl.Length + 1)..];

    private async Task<List<ImageResponse>> UploadAsync(string accessToken, Guid productId, int count)
    {
        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            [.. Enumerable.Range(0, count).Select(_ => ImagesApi.NewPhoto(40, 30))]);
        return await response.ReadImagesAsync();
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }

    private async Task<Guid> SaveProductAsync()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Type = ProductType.Excursion,
            Name = "Old Town Walk",
            Description = "Three hours through the historic centre",
            Price = 25.50m,
        };

        await QueryDatabaseAsync(db =>
        {
            db.Products.Add(product);
            return db.SaveChangesAsync(CancellationToken);
        });

        return product.Id;
    }
}
