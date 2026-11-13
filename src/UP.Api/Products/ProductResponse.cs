using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record ProductResponse(
    Guid Id,
    ProductType Type,
    string Name,
    string Description,
    string ShortDescription,
    decimal Price,
    JsonElement Attributes);
