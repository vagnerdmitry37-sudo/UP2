using Microsoft.EntityFrameworkCore;

using UP.Application.Products;
using UP.Domain.Products;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Products;

internal sealed class ProductService(AppDbContext dbContext) : IProductService
{
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

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        IReadOnlyList<Guid>? ids,
        CancellationToken cancellationToken)
    {
        if (ids is null)
        {
            return await dbContext.Products
                .AsNoTracking()
                .OrderBy(product => product.Id)
                .ToListAsync(cancellationToken);
        }

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
}
