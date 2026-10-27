using UP.Domain.Images;

namespace UP.Api.Images;

public sealed record ImageResponse(Guid Id, ImageEntityType EntityType, Guid EntityId, int SortOrder, string Url);
