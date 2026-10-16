using UP.Domain.Common;

namespace UP.Domain.Products;

public sealed record ProductDetails(ProductType Type, string Name, string Description, Money Price, string Attributes);
