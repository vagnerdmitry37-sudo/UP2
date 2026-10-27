using UP.Domain.Images;

namespace UP.Application.Images;

public sealed record PublicImage(Guid Id, ImageEntityType EntityType, Guid EntityId, int SortOrder, string Url);
