using UP.Domain.Products;

namespace UP.Domain.ProductTypes;

public sealed record ProductTypeDefinition(
    ProductType Type,
    string PriceLabel,
    IReadOnlyList<AttributeDefinition> Attributes);
