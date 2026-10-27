using Riok.Mapperly.Abstractions;

using UP.Application.Images;

namespace UP.Api.Images;

[Mapper]
internal static partial class ImageMapper
{
    public static partial IReadOnlyList<ImageResponse> ToResponses(this IReadOnlyList<PublicImage> images);
}
