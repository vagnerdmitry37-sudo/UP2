using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using UP.Application.Images;
using UP.Domain.Images;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Images;

internal sealed class ImageService(AppDbContext dbContext, IOptions<R2Options> options) : IImageService
{
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

        string publicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');

        return
        [
            .. images.Select(image => new PublicImage(
                image.Id,
                image.EntityType,
                image.EntityId,
                image.SortOrder,
                $"{publicBaseUrl}/{image.Key}")),
        ];
    }
}
