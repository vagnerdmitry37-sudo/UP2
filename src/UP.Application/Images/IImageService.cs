using UP.Domain.Images;

namespace UP.Application.Images;

public interface IImageService
{
    Task<CreateImagesResult> CreateAsync(
        ImageEntityType entityType,
        Guid entityId,
        IReadOnlyList<ImageUpload> uploads,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PublicImage>> GetAllAsync(
        ImageEntityType entityType,
        IReadOnlyList<Guid> entityIds,
        CancellationToken cancellationToken);
}
