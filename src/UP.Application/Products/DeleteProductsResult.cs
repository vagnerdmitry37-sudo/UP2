namespace UP.Application.Products;

public sealed record DeleteProductsResult(IReadOnlyList<Guid> MissingIds, IReadOnlyList<Guid> IdsInOrders);
