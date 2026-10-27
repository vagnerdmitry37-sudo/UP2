namespace UP.Application.Images;

public sealed record UpdateImageOrderResult(bool ListsEveryImage, IReadOnlyList<PublicImage> Images);
