using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record ProductResponse(
    Guid Id,
    ProductType Type,
    string Name,
    string Description,
    decimal Price,
    JsonElement Attributes);
