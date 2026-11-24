using Riok.Mapperly.Abstractions;

using UP.Application.Products;
using UP.Domain.Products;

namespace UP.Api.Products;

[Mapper]
internal static partial class ProductMapper
{
    [MapperIgnoreTarget(nameof(Product.Id))]
    public static partial Product ToProduct(this CreateProductRequest request);

    public static partial Product ToProduct(this UpdateProductRequest request);

    public static partial ProductResponse ToResponse(this Product product);

    public static partial IReadOnlyList<ProductResponse> ToResponses(this IReadOnlyList<Product> products);

    [MapNestedProperties(nameof(ProductDetails.Product))]
    public static partial ProductDetailsResponse ToResponse(this ProductDetails details);

    public static partial IReadOnlyList<ProductDetailsResponse> ToResponses(this IReadOnlyList<ProductDetails> products);

    public static partial ProductsQuery ToQuery(this GetProductsRequest request);

    public static partial ProductsPageResponse ToResponse(this ProductsPage page);
}
