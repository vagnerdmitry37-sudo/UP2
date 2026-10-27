namespace UP.Api.Products;

public sealed record ProductsPageResponse(IReadOnlyList<ProductResponse> Items, int TotalRecords);
