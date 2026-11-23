using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record UpdateProductRequest(
    Guid Id,
    [EnumDataType(typeof(ProductType))] ProductType Type,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(160)] string SeoDescription,
    [Range(0d, double.MaxValue)] decimal Price,
    JsonElement Attributes,
    [Required(AllowEmptyStrings = true), MaxLength(200)] string ShortDescription = "")
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ProductAttributesValidator.Validate(Type, Attributes);
}
