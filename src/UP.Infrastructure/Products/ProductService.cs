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
}
