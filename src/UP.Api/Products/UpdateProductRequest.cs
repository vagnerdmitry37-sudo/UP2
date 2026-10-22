using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record UpdateProductRequest(
    ProductType Type,
    string Name,
    string Description,
    decimal Price,
    JsonElement Attributes);
