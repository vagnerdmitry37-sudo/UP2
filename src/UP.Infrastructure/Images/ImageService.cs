using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UP.Application.Images;
using UP.Domain.Images;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Images;

internal sealed class ImageService(
    AppDbContext dbContext,
    IAmazonS3 s3,
    IOptions<R2Options> options,
    ILogger<ImageService> logger) : IImageService
{
    private const string WebpContentType = "image/webp";

    private readonly R2Options _r2 = options.Value;

    public async Task<CreateImagesResult> CreateAsync(
        ImageEntityType entityType,
        Guid entityId,
        IReadOnlyList<Stream> files,
        CancellationToken cancellationToken)
    {
        if (!await EntityExistsAsync(entityType, entityId, cancellationToken))
        {
            return new CreateImagesResult(false, [], []);
        }

        List<byte[]> webpFiles = [];
        List<int> invalidFileIndexes = [];

        for (int index = 0; index < files.Count; index++)
        {
            byte[]? webpFile = ImageEncoder.TryEncodeAsWebp(files[index]);

            if (webpFile is null)
            {
                invalidFileIndexes.Add(index);
            }
            else
            {
                webpFiles.Add(webpFile);
            }
        }

        if (invalidFileIndexes.Count > 0)
        {
            return new CreateImagesResult(true, invalidFileIndexes, []);
        }

        int lastSortOrder = await dbContext.Images
            .Where(image => image.EntityType == entityType && image.EntityId == entityId)
            .MaxAsync(image => (int?)image.SortOrder, cancellationToken) ?? -1;

        List<Image> images = [];

        foreach (byte[] webpFile in webpFiles)
        {
            Guid imageId = Guid.CreateVersion7();
            var image = new Image
            {
                Id = imageId,
                EntityType = entityType,
                EntityId = entityId,
                Key = $"{entityType}/{entityId}/{imageId}.webp",
                SortOrder = lastSortOrder + images.Count + 1,
            };

            using var content = new MemoryStream(webpFile);
            await s3.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = _r2.Bucket,
                    Key = image.Key,
                    InputStream = content,
                    ContentType = WebpContentType,
                    UseChunkEncoding = false,
                },
                cancellationToken);

            images.Add(image);
        }

        dbContext.Images.AddRange(images);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateImagesResult(true, [], [.. images.Select(ToPublicImage)]);
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

    public async Task<IReadOnlyList<Guid>> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        List<Image> images = await dbContext.Images
            .Where(image => ids.Contains(image.Id))
            .ToListAsync(cancellationToken);
        List<Guid> missingIds = [.. ids.Except(images.Select(image => image.Id))];

        if (missingIds.Count > 0)
        {
            return missingIds;
        }

        await RemoveWithFilesAsync(images, cancellationToken);

        return [];
    }

    public async Task DeleteAllAsync(
        ImageEntityType entityType,
        IReadOnlyList<Guid> entityIds,
        CancellationToken cancellationToken)
    {
        List<Image> images = await dbContext.Images
            .Where(image => image.EntityType == entityType && entityIds.Contains(image.EntityId))
            .ToListAsync(cancellationToken);

        if (images.Count > 0)
        {
            await RemoveWithFilesAsync(images, cancellationToken);
        }
    }

    private async Task RemoveWithFilesAsync(List<Image> images, CancellationToken cancellationToken)
    {
        dbContext.Images.RemoveRange(images);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await s3.DeleteObjectsAsync(
                new DeleteObjectsRequest
                {
                    BucketName = _r2.Bucket,
                    Objects = [.. images.Select(image => new KeyVersion { Key = image.Key })],
                },
                cancellationToken);
        }
        catch (AmazonServiceException exception)
        {
            logger.LogImageFilesNotDeleted(exception, images.Count);
        }
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
