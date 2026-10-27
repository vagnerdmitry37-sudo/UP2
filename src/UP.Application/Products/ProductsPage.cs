using UP.Domain.Products;

namespace UP.Application.Products;

public sealed record ProductsPage(IReadOnlyList<Product> Items, int TotalRecords);
