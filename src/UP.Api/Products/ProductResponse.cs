using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record ProductResponse(
    Guid Id,
    ProductType Type,
    string TypeLabel,
    string Name,
    string SeoDescription,
    string ShortDescription,
    decimal Price,
    JsonElement Attributes);
