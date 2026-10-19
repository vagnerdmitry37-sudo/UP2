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
}
