using System.Net;

using UP.Api.Auth;
using UP.Api.Images;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Images;

public sealed class UpdateImageOrderTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task New_order_is_returned_when_asking_for_the_images()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 3);
        Guid[] newOrder = [uploaded[2].Id, uploaded[0].Id, uploaded[1].Id];

        HttpResponseMessage response = await Client.UpdateImageOrderAsync(accessToken, productId, newOrder);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<ImageResponse> returned = await response.ReadImagesAsync();
        returned.Select(image => image.Id).ShouldBe(newOrder);
        returned.Select(image => image.SortOrder).ShouldBe([0, 1, 2]);
        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        listed.Select(image => image.Id).ShouldBe(newOrder);
    }

    [Fact]
    public async Task First_image_in_the_new_order_becomes_the_cover()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 2);

        await Client.UpdateImageOrderAsync(accessToken, productId, uploaded[1].Id, uploaded[0].Id);

        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        ImageResponse cover = listed[0];
        cover.Id.ShouldBe(uploaded[1].Id);
        cover.SortOrder.ShouldBe(0);
    }

    [Fact]
    public async Task List_missing_an_image_is_rejected_and_order_is_unchanged()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 3);

        HttpResponseMessage response = await Client.UpdateImageOrderAsync(
            accessToken,
            productId,
            uploaded[2].Id,
            uploaded[0].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveOrderAsync(productId, uploaded);
    }

    [Fact]
    public async Task Image_of_another_item_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        Guid otherProductId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 1);
        List<ImageResponse> otherUploaded = await UploadAsync(accessToken, otherProductId, count: 1);

        HttpResponseMessage response = await Client.UpdateImageOrderAsync(
            accessToken,
            productId,
            otherUploaded[0].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveOrderAsync(productId, uploaded);
    }

    [Fact]
    public async Task Same_image_twice_is_rejected()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 2);

        HttpResponseMessage response = await Client.UpdateImageOrderAsync(
            accessToken,
            productId,
            uploaded[1].Id,
            uploaded[1].Id,
            uploaded[0].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveOrderAsync(productId, uploaded);
    }

    [Fact]
    public async Task Changing_the_order_requires_sign_in()
    {
        string accessToken = await SignInAsync();
        Guid productId = await SaveProductAsync();
        List<ImageResponse> uploaded = await UploadAsync(accessToken, productId, count: 2);

        HttpResponseMessage response = await Client.UpdateImageOrderAsync(
            null,
            productId,
            uploaded[1].Id,
            uploaded[0].Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await ShouldHaveOrderAsync(productId, uploaded);
    }

    private async Task ShouldHaveOrderAsync(Guid productId, List<ImageResponse> expected)
    {
        List<ImageResponse> listed = await (await Client.GetImagesAsync("Product", productId)).ReadImagesAsync();
        listed.Select(image => image.Id).ShouldBe(expected.Select(image => image.Id));
    }

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
