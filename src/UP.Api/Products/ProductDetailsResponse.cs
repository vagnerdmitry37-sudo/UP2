using System.Text.Json;

using UP.Api.Images;
using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record ProductDetailsResponse(
    Guid Id,
    ProductType Type,
    string TypeLabel,
    string Name,
    string SeoDescription,
    string ShortDescription,
    decimal Price,
    JsonElement Attributes,
    IReadOnlyList<ImageResponse> Images);
