using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using UP.Application.Images;
using UP.Domain.Images;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Images;

internal sealed class ImageService(AppDbContext dbContext, IAmazonS3 s3, IOptions<R2Options> options) : IImageService
{
    private readonly R2Options _r2 = options.Value;

    public async Task<CreateImagesResult> CreateAsync(
        ImageEntityType entityType,
        Guid entityId,
        IReadOnlyList<ImageUpload> uploads,
        CancellationToken cancellationToken)
    {
        if (!await EntityExistsAsync(entityType, entityId, cancellationToken))
        {
            return new CreateImagesResult(false, []);
        }

        int lastSortOrder = await dbContext.Images
            .Where(image => image.EntityType == entityType && image.EntityId == entityId)
            .MaxAsync(image => (int?)image.SortOrder, cancellationToken) ?? -1;

        List<Image> images = [];

        foreach (ImageUpload upload in uploads)
        {
            Guid imageId = Guid.CreateVersion7();
            var image = new Image
            {
                Id = imageId,
                EntityType = entityType,
                EntityId = entityId,
                Key = $"{entityType}/{entityId}/{imageId}",
                SortOrder = lastSortOrder + images.Count + 1,
            };

            await s3.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = _r2.Bucket,
                    Key = image.Key,
                    InputStream = upload.Content,
                    ContentType = upload.ContentType,
                    UseChunkEncoding = false,
                },
                cancellationToken);

            images.Add(image);
        }

        dbContext.Images.AddRange(images);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateImagesResult(true, [.. images.Select(ToPublicImage)]);
    }

    public async Task<IReadOnlyList<PublicImage>> GetAllAsync(
        ImageEntityType entityType,
        IReadOnlyList<Guid> entityIds,
        CancellationToken cancellationToken)
    {
        List<Image> images = await dbContext.Images
            .AsNoTracking()
            .Where(image => image.EntityType == entityType && entityIds.Contains(image.EntityId))
            .OrderBy(image => image.EntityId)
            .ThenBy(image => image.SortOrder)
            .ToListAsync(cancellationToken);

        return [.. images.Select(ToPublicImage)];
    }

    private Task<bool> EntityExistsAsync(
        ImageEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) => entityType switch
        {
            ImageEntityType.Product => dbContext.Products.AnyAsync(product => product.Id == entityId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown image entity type."),
        };

    private PublicImage ToPublicImage(Image image) =>
        new(
            image.Id,
            image.EntityType,
            image.EntityId,
            image.SortOrder,
            $"{_r2.PublicBaseUrl.TrimEnd('/')}/{image.Key}");
}
