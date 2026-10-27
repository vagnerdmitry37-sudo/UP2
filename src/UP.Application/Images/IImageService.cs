using UP.Domain.Images;

namespace UP.Application.Images;

public interface IImageService
{
    Task<IReadOnlyList<PublicImage>> GetAllAsync(
        ImageEntityType entityType,
        IReadOnlyList<Guid> entityIds,
        CancellationToken cancellationToken);
}
