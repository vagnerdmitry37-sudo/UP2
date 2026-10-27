using Microsoft.EntityFrameworkCore;

using UP.Application.Products;
using UP.Domain.Products;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Products;

internal sealed class ProductService(AppDbContext dbContext) : IProductService
{
    private const string LikeEscape = @"\";

    public async Task<IReadOnlyList<Product>> CreateAsync(
        IReadOnlyList<Product> products,
        CancellationToken cancellationToken)
    {
        foreach (Product product in products)
        {
            product.Id = Guid.CreateVersion7();
        }

        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync(cancellationToken);

        return products;
    }

    public async Task<ProductsPage> GetAllAsync(ProductsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Product> matching = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.GlobalFilter))
        {
            string pattern = $"%{EscapeLikePattern(query.GlobalFilter.Trim())}%";
            matching = matching.Where(product =>
                EF.Functions.ILike(product.Name, pattern, LikeEscape)
                || EF.Functions.ILike(product.Description, pattern, LikeEscape));
        }

        int totalRecords = await matching.CountAsync(cancellationToken);
        List<Product> items = await Sort(matching, query.SortField, query.SortOrder)
            .Skip(query.First)
            .Take(query.Rows)
            .ToListAsync(cancellationToken);

        return new ProductsPage(items, totalRecords);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        List<Product> products = await dbContext.Products
            .AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);

        return [.. ids.Select(id => products.Find(product => product.Id == id)).OfType<Product>()];
    }

    public async Task<UpdateProductsResult> UpdateAsync(
        IReadOnlyList<Product> products,
        CancellationToken cancellationToken)
    {
        List<Guid> ids = products.Select(product => product.Id).ToList();
        List<Guid> existingIds = await dbContext.Products
            .Where(product => ids.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);
        List<Guid> missingIds = ids.Except(existingIds).ToList();

        if (missingIds.Count > 0)
        {
            return new UpdateProductsResult([], missingIds);
        }

        dbContext.Products.UpdateRange(products);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateProductsResult(products, []);
    }

    public async Task<IReadOnlyList<Guid>> DeleteAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        List<Guid> existingIds = await dbContext.Products
            .Where(product => ids.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);
        List<Guid> missingIds = ids.Except(existingIds).ToList();

        if (missingIds.Count > 0)
        {
            return missingIds;
        }

        await dbContext.Products
            .Where(product => ids.Contains(product.Id))
            .ExecuteDeleteAsync(cancellationToken);

        return [];
    }

    private static IQueryable<Product> Sort(
        IQueryable<Product> products,
        ProductSortField? sortField,
        int sortOrder)
    {
        bool descending = sortOrder < 0;

        IOrderedQueryable<Product> sorted = sortField switch
        {
            ProductSortField.Name => descending
                ? products.OrderByDescending(product => product.Name)
                : products.OrderBy(product => product.Name),
            ProductSortField.Type => descending
                ? products.OrderByDescending(product => product.Type)
                : products.OrderBy(product => product.Type),
            ProductSortField.Price => descending
                ? products.OrderByDescending(product => product.Price)
                : products.OrderBy(product => product.Price),
            _ => descending
                ? products.OrderByDescending(product => product.Id)
                : products.OrderBy(product => product.Id),
        };

        return sorted.ThenBy(product => product.Id);
    }

    private static string EscapeLikePattern(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
