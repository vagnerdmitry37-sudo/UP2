using System.Net;

using UP.Api.Images;
using UP.Api.IntegrationTests.TestHost;
using UP.Domain.Images;

namespace UP.Api.IntegrationTests.Images;

public sealed class GetImagesTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Images_are_returned_in_order_with_public_urls()
    {
        Guid productId = Guid.CreateVersion7();
        Image second = ImagesApi.NewImage(productId, sortOrder: 1);
        Image cover = ImagesApi.NewImage(productId, sortOrder: 0);
        Image third = ImagesApi.NewImage(productId, sortOrder: 2);
        await SaveImagesAsync(second, cover, third);

        HttpResponseMessage response = await Client.GetImagesAsync("Product", productId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<ImageResponse> images = await response.ReadImagesAsync();
        images.Select(image => image.Id).ShouldBe([cover.Id, second.Id, third.Id]);
        images[0].EntityType.ShouldBe(ImageEntityType.Product);
        images[0].EntityId.ShouldBe(productId);
        images[0].SortOrder.ShouldBe(0);
        images[0].Url.ShouldBe($"{ApiFactory.ImagesPublicBaseUrl}/{cover.Key}");
    }

    [Fact]
    public async Task Images_of_several_items_are_returned_in_one_request()
    {
        Guid firstProductId = Guid.CreateVersion7();
        Guid secondProductId = Guid.CreateVersion7();
        await SaveImagesAsync(
            ImagesApi.NewImage(firstProductId, sortOrder: 0),
            ImagesApi.NewImage(secondProductId, sortOrder: 0),
            ImagesApi.NewImage(secondProductId, sortOrder: 1));

        HttpResponseMessage response = await Client.GetImagesAsync("Product", firstProductId, secondProductId);

        List<ImageResponse> images = await response.ReadImagesAsync();
        images.Count(image => image.EntityId == firstProductId).ShouldBe(1);
        images.Count(image => image.EntityId == secondProductId).ShouldBe(2);
    }

    [Fact]
    public async Task Images_of_other_items_are_not_returned()
    {
        Guid productId = Guid.CreateVersion7();
        await SaveImagesAsync(ImagesApi.NewImage(Guid.CreateVersion7(), sortOrder: 0));

        HttpResponseMessage response = await Client.GetImagesAsync("Product", productId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadImagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Unknown_entity_type_is_rejected()
    {
        HttpResponseMessage response = await Client.GetImagesAsync("Banana", Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_entity_type_is_rejected()
    {
        HttpResponseMessage response = await Client.GetAsync(
            $"{ImagesApi.ImagesRoute}?entityIds={Guid.CreateVersion7()}",
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Request_without_entity_ids_is_rejected()
    {
        HttpResponseMessage response = await Client.GetImagesAsync("Product");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task More_than_100_entity_ids_are_rejected()
    {
        Guid[] entityIds = [.. Enumerable.Range(0, 101).Select(_ => Guid.CreateVersion7())];

        HttpResponseMessage response = await Client.GetImagesAsync("Product", entityIds);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task SaveImagesAsync(params Image[] images) =>
        await QueryDatabaseAsync(db =>
        {
            db.Images.AddRange(images);
            return db.SaveChangesAsync(CancellationToken);
        });
}
