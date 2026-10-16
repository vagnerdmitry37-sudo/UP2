using UP.Domain.Common;

namespace UP.Domain.Products;

public sealed record ProductDetails(string Type, string Name, string Description, Money Price, string Attributes);
