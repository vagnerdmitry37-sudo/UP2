using System.Net;
using System.Text;

using Microsoft.EntityFrameworkCore;

using SkiaSharp;

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
    public async Task Uploaded_image_is_stored_as_webp_with_public_url()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(accessToken, productId, ImagesApi.NewPhoto());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        ImageResponse image = (await response.ReadImagesAsync()).ShouldHaveSingleItem();
        image.EntityType.ShouldBe(ImageEntityType.Product);
        image.EntityId.ShouldBe(productId);
        image.SortOrder.ShouldBe(0);
        image.Url.ShouldStartWith($"{ApiFactory.ImagesPublicBaseUrl}/Product/{productId}/");
        image.Url.ShouldEndWith(".webp");
        StoredFile stored = (await ReadStoredFileAsync(KeyOf(image))).ShouldNotBeNull();
        stored.ContentType.ShouldBe("image/webp");
        using SKBitmap webp = ImagesApi.DecodeWebp(stored.Content);
        webp.Width.ShouldBe(400);
        webp.Height.ShouldBe(300);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task Png_and_webp_images_are_accepted(SKEncodedImageFormat format)
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewPhoto(format: format));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Large_image_is_shrunk_to_1600_pixels_on_the_longest_side()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewPhoto(3200, 2000));

        ImageResponse image = (await response.ReadImagesAsync()).ShouldHaveSingleItem();
        StoredFile stored = (await ReadStoredFileAsync(KeyOf(image))).ShouldNotBeNull();
        using SKBitmap webp = ImagesApi.DecodeWebp(stored.Content);
        webp.Width.ShouldBe(1600);
        webp.Height.ShouldBe(1000);
    }

    [Fact]
    public async Task Phone_photo_is_stored_upright()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewPhoneTurnedRightPhoto(400, 300));

        ImageResponse image = (await response.ReadImagesAsync()).ShouldHaveSingleItem();
        StoredFile stored = (await ReadStoredFileAsync(KeyOf(image))).ShouldNotBeNull();
        using SKBitmap webp = ImagesApi.DecodeWebp(stored.Content);
        webp.Width.ShouldBe(300);
        webp.Height.ShouldBe(400);
        SKColor top = webp.GetPixel(150, 50);
        SKColor bottom = webp.GetPixel(150, 350);
        top.Red.ShouldBeGreaterThan(top.Blue);
        bottom.Blue.ShouldBeGreaterThan(bottom.Red);
    }

    [Fact]
    public async Task Uploaded_images_are_listed_for_the_item()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        List<ImageResponse> uploaded = await (await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewPhoto(),
            ImagesApi.NewPhoto())).ReadImagesAsync();

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
            ImagesApi.NewPhoto(),
            ImagesApi.NewPhoto());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.ReadImagesAsync()).Select(image => image.SortOrder).ShouldBe([5, 6]);
    }

    [Fact]
    public async Task One_fake_file_rejects_the_whole_upload_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            ImagesApi.NewPhoto(),
            Encoding.UTF8.GetBytes("this is not a picture"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        int savedCount = await QueryDatabaseAsync(db => db.Images.CountAsync(CancellationToken));
        savedCount.ShouldBe(0);
        (await CountStoredFilesAsync($"Product/{productId}/")).ShouldBe(0);
    }

    [Fact]
    public async Task Broken_image_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        byte[] photo = ImagesApi.NewPhoto();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            photo[..(photo.Length / 2)]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Gif_image_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(accessToken, productId, ImagesApi.TinyGif);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_for_missing_item_is_refused_and_saves_nothing()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            Guid.CreateVersion7(),
            ImagesApi.NewPhoto());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
            new byte[CreateImagesRequest.MaxFileBytes + 1]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task More_than_10_files_are_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        byte[] photo = ImagesApi.NewPhoto(10, 10);

        HttpResponseMessage response = await Client.CreateImagesAsync(
            accessToken,
            productId,
            [.. Enumerable.Repeat(photo, CreateImagesRequest.MaxFiles + 1)]);

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

        HttpResponseMessage response = await Client.CreateImagesAsync(null, productId, ImagesApi.NewPhoto());

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
