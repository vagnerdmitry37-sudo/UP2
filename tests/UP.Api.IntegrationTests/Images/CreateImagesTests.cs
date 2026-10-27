using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.Images;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Domain.Images;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Images;

public sealed class CreateImagesTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Uploaded_image_is_saved_to_storage_and_returned_with_public_url()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        (byte[] content, string contentType) = ImagesApi.NewFile("image/png");

        HttpResponseMessage response = await Client.CreateImagesAsync(accessToken, productId, (content, contentType));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        ImageResponse image = (await response.ReadImagesAsync()).ShouldHaveSingleItem();
        image.EntityType.ShouldBe(ImageEntityType.Product);
        image.EntityId.ShouldBe(productId);
        image.SortOrder.ShouldBe(0);
        image.Url.ShouldStartWith($"{ApiFactory.ImagesPublicBaseUrl}/Product/{productId}/");
        StoredFile stored = (await ReadStoredFileAsync(KeyOf(image))).ShouldNotBeNull();
        stored.Content.ShouldBe(content);
        stored.ContentType.ShouldBe("image/png");
    }

    [Fact]
    public async Task Uploaded_images_are_listed_for_the_item()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        List<ImageResponse> uploaded = await (await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewFile(),
            ImagesApi.NewFile())).ReadImagesAsync();

        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        listed.Select(image => image.Id).ShouldBe(uploaded.Select(image => image.Id));
    }

    [Fact]
    public async Task New_images_go_to_the_end_of_the_list()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        await QueryDatabaseAsync(db =>
        {
            db.Images.Add(ImagesApi.NewImage(productId, sortOrder: 4));
            return db.SaveChangesAsync(CancellationToken);
        });

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewFile(),
            ImagesApi.NewFile());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.ReadImagesAsync()).Select(image => image.SortOrder).ShouldBe([5, 6]);
    }

    [Fact]
    public async Task Upload_for_missing_item_is_refused_and_saves_nothing()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            Guid.CreateVersion7(),
            ImagesApi.NewFile());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        int savedCount = await QueryDatabaseAsync(db => db.Images.CountAsync(CancellationToken));
        savedCount.ShouldBe(0);
    }

    [Fact]
    public async Task File_that_is_not_jpeg_png_or_webp_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewFile(),
            ImagesApi.NewFile("image/gif"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        int savedCount = await QueryDatabaseAsync(db => db.Images.CountAsync(CancellationToken));
        savedCount.ShouldBe(0);
    }

    [Fact]
    public async Task File_larger_than_10_mb_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewFile(length: (int)CreateImagesRequest.MaxFileBytes + 1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task More_than_10_files_are_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            [.. Enumerable.Range(0, CreateImagesRequest.MaxFiles + 1).Select(_ => ImagesApi.NewFile())]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Request_without_files_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(accessToken, productId);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_requires_sign_in()
    {
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(null, productId, ImagesApi.NewFile());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string KeyOf(ImageResponse image) => image.Url[(ApiFactory.ImagesPublicBaseUrl.Length + 1)..];

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
