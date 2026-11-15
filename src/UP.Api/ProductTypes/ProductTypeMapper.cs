using Riok.Mapperly.Abstractions;

using UP.Domain.ProductTypes;

namespace UP.Api.ProductTypes;

[Mapper]
internal static partial class ProductTypeMapper
{
    public static partial IReadOnlyList<ProductTypeResponse> ToResponses(this IReadOnlyList<ProductTypeDefinition> definitions);

    private static partial ProductTypeResponse ToResponse(ProductTypeDefinition definition);

    private static partial AttributeDefinitionResponse ToResponse(AttributeDefinition attribute);
}
