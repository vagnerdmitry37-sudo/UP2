using UP.Domain.Products;

namespace UP.Application.Products;

public interface IProductService
{
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken);
}
