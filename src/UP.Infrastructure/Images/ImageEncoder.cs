using SkiaSharp;

namespace UP.Infrastructure.Images;

internal static class ImageEncoder
{
    private const int MaxSide = 1600;
    private const int WebpQuality = 80;
    private const long MaxSourcePixels = 50_000_000;

    private static readonly SKEncodedImageFormat[] SupportedFormats =
        [SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Png, SKEncodedImageFormat.Webp];

    public static byte[]? TryEncodeAsWebp(Stream file)
    {
        using SKCodec? codec = SKCodec.Create(file);

        if (codec is null
            || !SupportedFormats.Contains(codec.EncodedFormat)
            || ((long)codec.Info.Width * codec.Info.Height) > MaxSourcePixels)
        {
            return null;
        }

        var decodedInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var decoded = new SKBitmap(decodedInfo);

        if (codec.GetPixels(decodedInfo, decoded.GetPixels()) != SKCodecResult.Success)
        {
            return null;
        }

        using SKBitmap upright = RotateAndShrink(decoded, codec.EncodedOrigin);
        using SKData webp = upright.Encode(SKEncodedImageFormat.Webp, WebpQuality);

        return webp.ToArray();
    }

    private static SKBitmap RotateAndShrink(SKBitmap source, SKEncodedOrigin origin)
    {
        bool isTurnedSideways = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;
        int uprightWidth = isTurnedSideways ? source.Height : source.Width;
        int uprightHeight = isTurnedSideways ? source.Width : source.Height;
        float scale = Math.Min(1f, (float)MaxSide / Math.Max(uprightWidth, uprightHeight));

        var target = new SKBitmap(
            Math.Max(1, (int)Math.Round(uprightWidth * scale)),
            Math.Max(1, (int)Math.Round(uprightHeight * scale)),
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var canvas = new SKCanvas(target);
        using var image = SKImage.FromBitmap(source);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(SKMatrix.CreateScale(scale, scale).PreConcat(UprightTransform(origin, source.Width, source.Height)));
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        return target;
    }

    private static SKMatrix UprightTransform(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
