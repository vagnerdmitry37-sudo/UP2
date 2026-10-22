using UP.Domain.Products;

namespace UP.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<Product>> CreateAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetAllAsync(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken);

    Task<UpdateProductsResult> UpdateAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken);
}
