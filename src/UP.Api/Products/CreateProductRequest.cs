using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Products;

public sealed record CreateProductRequest(
    [EnumDataType(typeof(ProductType))] ProductType Type,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(5000)] string Description,
    [Range(0d, double.MaxValue)] decimal Price,
    JsonElement Attributes);
