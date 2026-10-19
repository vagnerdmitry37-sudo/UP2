using System.Text.Json;

using Riok.Mapperly.Abstractions;

using UP.Domain.Products;

namespace UP.Api.Products;

[Mapper]
internal static partial class ProductMapper
{
    [MapperIgnoreTarget(nameof(Product.Id))]
    public static partial Product ToProduct(this CreateProductRequest request);

    [MapProperty(nameof(Product.Attributes), nameof(ProductResponse.Attributes), Use = nameof(ParseAttributes))]
    public static partial ProductResponse ToResponse(this Product product);

    private static JsonElement ParseAttributes(string attributes) => JsonSerializer.Deserialize<JsonElement>(attributes);
}
