using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using SkiaSharp;

using UP.Api.Images;
using UP.Domain.Images;

namespace UP.Api.IntegrationTests.Images;

internal static class ImagesApi
{
    public const string ImagesRoute = "api/images";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Image NewImage(Guid entityId, int sortOrder) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EntityType = ImageEntityType.Product,
            EntityId = entityId,
            Key = $"Product/{entityId}/{Guid.CreateVersion7()}.webp",
            SortOrder = sortOrder,
        };

    public static readonly byte[] TinyGif = Convert.FromBase64String("R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

    private static readonly byte[] ExifTurnedRightSegment =
    [
        0xFF, 0xE1, 0x00, 0x22,
        (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
        (byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
        0x00, 0x01,
        0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x06, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
    ];

    public static byte[] NewPhoto(
        int width = 400,
        int height = 300,
        SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Red);
            using var blue = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(width / 2f, 0, width / 2f, height, blue);
        }

        using SKData encoded = bitmap.Encode(format, 90);
        return encoded.ToArray();
    }

    public static byte[] NewPhoneTurnedRightPhoto(int width = 400, int height = 300)
    {
        byte[] jpeg = NewPhoto(width, height);
        return [.. jpeg[..2], .. ExifTurnedRightSegment, .. jpeg[2..]];
    }

    public static SKBitmap DecodeWebp(byte[] content)
    {
        using var codec = SKCodec.Create(new MemoryStream(content));
        codec.EncodedFormat.ShouldBe(SKEncodedImageFormat.Webp);
        return SKBitmap.Decode(codec);
    }

    public static async Task<HttpResponseMessage> CreateImagesAsync(
        this HttpClient client,
        string? accessToken,
        Guid entityId,
        params byte[][] files)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Product"), "entityType" },
            { new StringContent(entityId.ToString()), "entityId" },
        };

        foreach (byte[] content in files)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(file, "files", "photo.jpg");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ImagesRoute) { Content = form };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<HttpResponseMessage> DeleteImagesAsync(
        this HttpClient client,
        string? accessToken,
        params Guid[] ids)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{ImagesRoute}?{string.Join("&", ids.Select(id => $"ids={id}"))}");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static Task<HttpResponseMessage> GetImagesAsync(
        this HttpClient client,
        string entityType,
        params Guid[] entityIds) =>
        client.GetAsync(
            $"{ImagesRoute}?entityType={entityType}{string.Concat(entityIds.Select(id => $"&entityIds={id}"))}",
            CancellationToken);

    public static async Task<List<ImageResponse>> ReadImagesAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<ImageResponse>>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no images.");
}
