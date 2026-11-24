using System.ComponentModel.DataAnnotations;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record UpdateProductRequest(
    Guid Id,
    [EnumDataType(typeof(ProductType))] ProductType Type,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(160)] string SeoDescription,
    [Range(0d, double.MaxValue)] decimal Price,
    [Required(AllowEmptyStrings = true), MaxLength(200)] string ShortDescription = "");
