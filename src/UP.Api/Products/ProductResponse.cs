using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record ProductResponse(
    Guid Id,
    ProductType Type,
    string Name,
    string SeoDescription,
    string ShortDescription,
    string Description,
    decimal Price,
    JsonElement Attributes);
