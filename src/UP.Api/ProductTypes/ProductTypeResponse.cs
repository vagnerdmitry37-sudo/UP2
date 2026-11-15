using UP.Domain.Products;

namespace UP.Api.ProductTypes;

public sealed record ProductTypeResponse(
    ProductType Type,
    string Label,
    string PriceLabel,
    IReadOnlyList<AttributeDefinitionResponse> Attributes);
