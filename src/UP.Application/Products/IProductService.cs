using UP.Domain.Products;

namespace UP.Application.Products;

public interface IProductService
{
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken);
}
