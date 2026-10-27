using UP.Domain.Products;

namespace UP.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<Product>> CreateAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken);

    Task<ProductsPage> GetAllAsync(ProductsQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);

    Task<UpdateProductsResult> UpdateAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
