namespace UP.Application.Images;

public sealed record CreateImagesResult(bool EntityExists, IReadOnlyList<PublicImage> Created);
