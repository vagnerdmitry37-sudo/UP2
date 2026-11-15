using System.Text.Json;

using Riok.Mapperly.Abstractions;

using UP.Application.Products;
using UP.Domain.Products;
using UP.Domain.ProductTypes;

namespace UP.Api.Products;

[Mapper]
internal static partial class ProductMapper
{
    [MapperIgnoreTarget(nameof(Product.Id))]
    public static partial Product ToProduct(this CreateProductRequest request);

    public static partial Product ToProduct(this UpdateProductRequest request);

    [MapProperty(nameof(Product.Attributes), nameof(ProductResponse.Attributes), Use = nameof(ParseAttributes))]
    [MapProperty(nameof(Product.Type), nameof(ProductResponse.TypeLabel), Use = nameof(TypeLabelOf))]
    public static partial ProductResponse ToResponse(this Product product);

    public static partial IReadOnlyList<ProductResponse> ToResponses(this IReadOnlyList<Product> products);

    public static partial ProductsQuery ToQuery(this GetProductsRequest request);

    public static partial ProductsPageResponse ToResponse(this ProductsPage page);

    private static JsonElement ParseAttributes(string attributes) => JsonSerializer.Deserialize<JsonElement>(attributes);

    private static string TypeLabelOf(ProductType type) => ProductTypeCatalog.For(type).Label;
}
