namespace UP.Application.Products;

public sealed record ProductsQuery(
    int First,
    int Rows,
    ProductSortField? SortField,
    int SortOrder,
    string? GlobalFilter);
