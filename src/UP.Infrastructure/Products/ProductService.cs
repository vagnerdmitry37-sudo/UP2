using Microsoft.EntityFrameworkCore;

using UP.Application.Products;
using UP.Domain.Products;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Products;

internal sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken)
    {
        product.Id = Guid.CreateVersion7();

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return product;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

    public async Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Products.AnyAsync(saved => saved.Id == product.Id, cancellationToken);

        if (!exists)
        {
            return null;
        }

        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return product;
    }
}
