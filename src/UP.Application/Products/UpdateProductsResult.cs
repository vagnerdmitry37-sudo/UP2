using UP.Domain.Products;

namespace UP.Application.Products;

public sealed record UpdateProductsResult(IReadOnlyList<Product> Updated, IReadOnlyList<Guid> MissingIds);
