namespace UP.Application.Images;

public sealed record CreateImagesResult(
    bool EntityExists,
    IReadOnlyList<int> InvalidFileIndexes,
    IReadOnlyList<PublicImage> Created);
